using System.Security.Claims;
using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Attendance.Contracts;
using Attendance.Api.Modules.Attendance.Domain;
using Attendance.Api.Modules.Auditing.Application;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Api.Modules.Attendance.Application;

public sealed class AttendanceMarkAdministrationService(
    AttendanceDbContext database, AttendanceTimeZone timeZone, IAuditWriter audit)
{
    public async Task<IResult> ListAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken)
    {
        if (!await database.Employees.AnyAsync(x => x.Id == employeeId, cancellationToken))
            return TypedResults.NotFound();

        var start = timeZone.GetStartOfDay(date);
        var end = timeZone.GetStartOfNextDay(date);
        var marks = await database.AttendanceMarks.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.OccurredAt >= start && x.OccurredAt < end)
            .OrderBy(x => x.OccurredAt).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        return TypedResults.Ok(marks.Select(ToHistoryItem).ToArray());
    }

    public async Task<IResult> ChangeAsync(Guid employeeId, Guid? originalMarkId, AttendanceMarkChange request,
        ClaimsPrincipal actor, CancellationToken cancellationToken)
    {
        var reason = request.Reason?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length > 500)
            return Invalid("reason", "Ingresa un motivo de hasta 500 caracteres.");

        var now = DateTimeOffset.UtcNow;
        var addMark = originalMarkId is null || request.Date.HasValue || request.Time.HasValue || request.Type is not null;
        var occurredAt = default(DateTimeOffset);
        var type = default(AttendanceMarkType);
        if (addMark)
        {
            if (request.Date is not { } date || !AttendanceRequestValidator.ValidateDate(date).IsValid || request.Time is not { } time)
                return Invalid("date", "Ingresa una fecha y hora válidas.");
            if (!Enum.TryParse(request.Type, true, out type) || !Enum.IsDefined(type)
                || !string.Equals(request.Type, type.ToString(), StringComparison.OrdinalIgnoreCase))
                return Invalid("type", "Selecciona un tipo de marcación válido.");

            var localTime = date.ToDateTime(time);
            if (timeZone.TimeZone.IsInvalidTime(localTime) || timeZone.TimeZone.IsAmbiguousTime(localTime))
                return Invalid("time", "La hora es ambigua o no existe en la zona horaria de asistencia.");
            occurredAt = TimeZoneInfo.ConvertTimeToUtc(localTime, timeZone.TimeZone);
            if (occurredAt > now) return Invalid("time", "La marcación no puede estar en el futuro.");
        }

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await AttendanceEmployeeLock.AcquireAsync(database, employeeId, cancellationToken);
        var employee = await database.Employees.AsNoTracking().SingleOrDefaultAsync(x => x.Id == employeeId, cancellationToken);
        if (employee is null) return TypedResults.NotFound();
        if (addMark && (request.Date < employee.HireDate || request.Date > employee.TerminationDate))
            return Invalid("date", "La fecha debe pertenecer al período laboral del empleado.");

        AttendanceMark? original = null;
        if (originalMarkId is { } markId)
        {
            original = await database.AttendanceMarks.IgnoreQueryFilters().SingleOrDefaultAsync(
                x => x.Id == markId && x.EmployeeId == employeeId, cancellationToken);
            if (original is null) return TypedResults.NotFound();
            if (original.VoidedAt.HasValue) return TypedResults.Conflict(new { title = "La marcación ya fue anulada." });
        }

        AttendanceMark? replacement = null;
        if (addMark)
        {
            var duplicate = await database.AttendanceMarks.AnyAsync(x => x.EmployeeId == employeeId && x.Id != originalMarkId
                && x.OccurredAt == occurredAt && x.Type == type, cancellationToken);
            if (duplicate) return TypedResults.Conflict(new { title = "Ya existe una marcación igual." });

            replacement = AttendanceMark.CreateManual(employeeId, occurredAt, type, reason, originalMarkId);
            database.AttendanceMarks.Add(replacement);
        }

        original?.Void(now, reason);
        var changedMark = (original ?? replacement)!;
        var action = original is null ? "CreateManualAttendanceMark"
            : replacement is null ? "VoidAttendanceMark" : "CorrectAttendanceMark";
        audit.Prepare(actor, action, nameof(AttendanceMark), changedMark.Id.ToString(), new { employeeId, replacementId = replacement?.Id });
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Ok(ToHistoryItem(replacement ?? changedMark));
    }

    private AttendanceMarkHistoryItem ToHistoryItem(AttendanceMark mark) => new(mark.Id, mark.OccurredAt,
        TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(mark.OccurredAt, timeZone.TimeZone).DateTime), mark.Type.ToString(),
        mark.Source.ToString(), mark.ManualReason, mark.VoidedAt, mark.VoidReason, mark.ReplacesMarkId);

    private static IResult Invalid(string field, string message)
        => TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
