using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Attendance.Contracts;
using Attendance.Api.Modules.Attendance.Domain;
using Attendance.Api.Modules.Checkpoints.Application;
using Attendance.Api.Modules.Checkpoints.Contracts;
using Attendance.Api.Modules.Checkpoints.Domain;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Api.Modules.Attendance.Application;

public enum AttendanceCaptureStatus { Success, InvalidToken, ExpiredToken, InactiveCheckpoint, Replay, InvalidAction, EmployeeUnavailable }
public sealed record AttendanceCaptureResult<T>(AttendanceCaptureStatus Status, T? Value = default, string? Message = null);

public sealed class AttendanceCaptureService(
    AttendanceDbContext dbContext,
    AttendanceTimeZone attendanceTimeZone,
    CheckpointQrService qrService)
{
    public async Task<AttendanceCaptureResult<AttendanceCaptureResolutionResponse>> ResolveAsync(Guid employeeId, string token, CancellationToken cancellationToken)
    {
        var occurredAt = DateTimeOffset.UtcNow;
        if (!await CanMarkAsync(employeeId, occurredAt, cancellationToken)) return new(AttendanceCaptureStatus.EmployeeUnavailable);
        var checkpointResult = await ResolveCheckpointAsync(token, cancellationToken);
        if (checkpointResult.Status is { } status) return new(status);
        var checkpoint = checkpointResult.Checkpoint!;
        if (checkpoint is null || !checkpoint.IsActive) return new(AttendanceCaptureStatus.InactiveCheckpoint);
        var actions = await GetAvailableActionsAsync(employeeId, checkpoint.Type, occurredAt, cancellationToken);
        var message = actions.Count == 0
            ? "No hay una marcación disponible para este checkpoint en este momento."
            : actions.Count == 1 ? "Acción disponible." : "Selecciona la acción que deseas registrar.";
        return new(AttendanceCaptureStatus.Success, new(ToSummary(checkpoint), actions.Select(x => x.ToString()).ToArray(), message));
    }

    public async Task<AttendanceCaptureResult<AttendanceCaptureMarkResponse>> MarkAsync(Guid employeeId, string token, string action, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AttendanceMarkType>(action, true, out var requestedAction) || !Enum.IsDefined(requestedAction))
            return new(AttendanceCaptureStatus.InvalidAction, Message: "La acción solicitada no es válida.");

        await using var transaction = dbContext.Database.IsNpgsql()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (transaction is not null) await AttendanceEmployeeLock.AcquireAsync(dbContext, employeeId, cancellationToken);

        var checkpointResult = await ResolveCheckpointAsync(token, cancellationToken);
        if (checkpointResult.Status is { } status) return new(status);
        var checkpoint = checkpointResult.Checkpoint!;
        if (checkpoint is null || !checkpoint.IsActive) return new(AttendanceCaptureStatus.InactiveCheckpoint);
        var occurredAt = DateTimeOffset.UtcNow;
        if (!await CanMarkAsync(employeeId, occurredAt, cancellationToken)) return new(AttendanceCaptureStatus.EmployeeUnavailable);
        var actions = await GetAvailableActionsAsync(employeeId, checkpoint.Type, occurredAt, cancellationToken);
        if (!actions.Contains(requestedAction))
            return new(AttendanceCaptureStatus.InvalidAction, Message: "Esta marcación ya no es válida para la secuencia actual.");
        if (!qrService.TryReserve(checkpointResult.Payload!, employeeId))
            return new(AttendanceCaptureStatus.Replay, Message: "Este código QR ya fue usado para registrar una marcación.");

        try
        {
            var source = checkpoint.QrMode == CheckpointQrMode.Static ? AttendanceSource.StaticQr : AttendanceSource.DynamicQr;
            var mark = AttendanceMark.Create(employeeId, occurredAt, requestedAction, source, checkpoint.Id);
            dbContext.AttendanceMarks.Add(mark);
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new(AttendanceCaptureStatus.Success, new(mark.Id, mark.Type.ToString(), mark.OccurredAt, ToSummary(checkpoint), MessageFor(mark.Type)));
        }
        catch
        {
            qrService.Release(checkpointResult.Payload!, employeeId);
            throw;
        }
    }

    public async Task<IReadOnlyCollection<MyAttendanceMarkResponse>> GetTodayAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var today = attendanceTimeZone.GetLocalDate(DateTimeOffset.UtcNow);
        var start = attendanceTimeZone.GetStartOfDay(today);
        var end = attendanceTimeZone.GetStartOfNextDay(today);
        return await (
            from mark in dbContext.AttendanceMarks.AsNoTracking()
            join checkpoint in dbContext.Checkpoints.AsNoTracking() on mark.CheckpointId equals (Guid?)checkpoint.Id into checkpoints
            from checkpoint in checkpoints.DefaultIfEmpty()
            where mark.EmployeeId == employeeId && mark.OccurredAt >= start && mark.OccurredAt < end
            orderby mark.OccurredAt
            select new MyAttendanceMarkResponse(mark.Id, mark.Type.ToString(), mark.OccurredAt,
                checkpoint == null ? null : new CheckpointSummaryResponse(checkpoint.Id, checkpoint.Code, checkpoint.Name, checkpoint.Type.ToString()))
        ).ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<AttendanceMarkType>> GetAvailableActionsAsync(Guid employeeId, CheckpointType checkpointType, DateTimeOffset occurredAt, CancellationToken cancellationToken)
    {
        var today = attendanceTimeZone.GetLocalDate(occurredAt);
        var start = attendanceTimeZone.GetStartOfDay(today);
        var end = attendanceTimeZone.GetStartOfNextDay(today);
        var marks = await dbContext.AttendanceMarks.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.OccurredAt >= start && x.OccurredAt < end)
            .OrderBy(x => x.OccurredAt).ToListAsync(cancellationToken);

        return AttendanceCaptureSequence.Resolve(marks.Select(x => x.Type).ToArray(), checkpointType);
    }

    private Task<bool> CanMarkAsync(Guid employeeId, DateTimeOffset occurredAt, CancellationToken cancellationToken)
    {
        var date = attendanceTimeZone.GetLocalDate(occurredAt);
        return dbContext.Employees.AnyAsync(x => x.Id == employeeId && x.IsActive && x.HireDate <= date
            && (x.TerminationDate == null || x.TerminationDate >= date), cancellationToken);
    }

    private async Task<ResolvedCheckpoint> ResolveCheckpointAsync(string token, CancellationToken cancellationToken)
    {
        if (qrService.IsStaticToken(token))
        {
            var staticCheckpoint = await dbContext.Checkpoints.AsNoTracking()
                .SingleOrDefaultAsync(x => x.StaticQrToken == token, cancellationToken);
            if (staticCheckpoint is null) return new(AttendanceCaptureStatus.InvalidToken);
            if (staticCheckpoint.QrMode != CheckpointQrMode.Static) return new(AttendanceCaptureStatus.InvalidToken);
            return new(staticCheckpoint, qrService.CreateStaticPayload(staticCheckpoint.Id));
        }

        var validation = qrService.Validate(token);
        if (validation.Status != CheckpointQrValidationStatus.Valid) return new(MapTokenStatus(validation.Status));
        if (validation.Payload!.Mode != CheckpointQrMode.Dynamic) return new(AttendanceCaptureStatus.InvalidToken);
        var checkpoint = await dbContext.Checkpoints.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == validation.Payload!.CheckpointId, cancellationToken);
        if (checkpoint is null || checkpoint.QrMode != validation.Payload!.Mode) return new(AttendanceCaptureStatus.InvalidToken);
        return new(checkpoint, validation.Payload);
    }

    private sealed record ResolvedCheckpoint(Checkpoint? Checkpoint = null, CheckpointQrPayload? Payload = null, AttendanceCaptureStatus? Status = null)
    {
        public ResolvedCheckpoint(AttendanceCaptureStatus status) : this(null, null, status) { }
        public ResolvedCheckpoint(Checkpoint checkpoint, CheckpointQrPayload payload) : this(checkpoint, payload, null) { }
    }

    private static AttendanceCaptureStatus MapTokenStatus(CheckpointQrValidationStatus status) => status switch
    {
        CheckpointQrValidationStatus.Expired => AttendanceCaptureStatus.ExpiredToken,
        CheckpointQrValidationStatus.Replayed => AttendanceCaptureStatus.Replay,
        _ => AttendanceCaptureStatus.InvalidToken
    };
    private static CheckpointSummaryResponse ToSummary(Checkpoint checkpoint) => new(checkpoint.Id, checkpoint.Code, checkpoint.Name, checkpoint.Type.ToString());
    private static string MessageFor(AttendanceMarkType type) => type switch
    {
        AttendanceMarkType.Entry => "Entrada registrada.", AttendanceMarkType.LunchStart => "Inicio de almuerzo registrado.", AttendanceMarkType.LunchEnd => "Fin de almuerzo registrado.", AttendanceMarkType.Exit => "Salida registrada.", AttendanceMarkType.CommissionExit => "Salida a comisión registrada.", AttendanceMarkType.CommissionReturn => "Regreso de comisión registrado.", AttendanceMarkType.OtherExit => "Otra salida registrada.", AttendanceMarkType.OtherReturn => "Retorno registrado.", AttendanceMarkType.OvertimeStart => "Inicio de horas extra registrado.", AttendanceMarkType.OvertimeEnd => "Fin de horas extra registrado.", _ => "Marcación registrada."
    };
}
