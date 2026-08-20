using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Absences.Contracts;
using Attendance.Api.Modules.Absences.Domain;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Api.Modules.Absences.Application;

public sealed class AbsenceService(AttendanceDbContext dbContext)
{
    public async Task<IReadOnlyList<AbsenceResponse>> ListAsync(
        AbsenceQueryFilters filters,
        CancellationToken cancellationToken)
        => await BuildQuery(filters)
            .OrderByDescending(x => x.Period.Start)
            .ThenByDescending(x => x.Period.End)
            .Select(MapExpression())
            .ToListAsync(cancellationToken);

    public Task<AbsenceResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
        => dbContext.Absences
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(MapExpression())
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<AbsenceEmployeeHistoryResult> GetEmployeeHistoryAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var employeeExists = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(x => x.Id == employeeId, cancellationToken);

        if (!employeeExists)
        {
            return new AbsenceEmployeeHistoryResult(
                AbsenceEmployeeHistoryStatus.EmployeeNotFound);
        }

        var history = await ListAsync(
            new AbsenceQueryFilters(employeeId, null, null, null, null),
            cancellationToken);

        return new AbsenceEmployeeHistoryResult(
            AbsenceEmployeeHistoryStatus.Success,
            history);
    }

    public async Task<AbsenceWriteResult<AbsenceResponse>> CreateAsync(
        CreateAbsenceCommand command,
        CancellationToken cancellationToken)
    {
        var employee = await dbContext.Employees
            .AsNoTracking()
            .Where(x => x.Id == command.EmployeeId)
            .Select(x => new
            {
                x.Id,
                x.IsActive
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (employee is null)
        {
            return new AbsenceWriteResult<AbsenceResponse>(
                AbsenceWriteStatus.EmployeeNotFound);
        }

        if (!employee.IsActive)
        {
            return new AbsenceWriteResult<AbsenceResponse>(
                AbsenceWriteStatus.EmployeeInactive);
        }

        if (await HasActiveOverlapAsync(
                command.EmployeeId,
                command.Period,
                exclusionId: null,
                cancellationToken))
        {
            return new AbsenceWriteResult<AbsenceResponse>(
                AbsenceWriteStatus.OverlapConflict);
        }

        var absence = Absence.Create(
            command.EmployeeId,
            command.Period,
            command.Type,
            command.Reason,
            command.Notes);

        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AbsenceWriteResult<AbsenceResponse>(
            AbsenceWriteStatus.Success,
            Map(absence));
    }

    public async Task<AbsenceWriteResult<AbsenceResponse>> UpdateAsync(
        Guid id,
        UpdateAbsenceCommand command,
        CancellationToken cancellationToken)
    {
        var absence = await dbContext.Absences
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (absence is null)
        {
            return new AbsenceWriteResult<AbsenceResponse>(
                AbsenceWriteStatus.NotFound);
        }

        if (absence.Version != command.ExpectedVersion)
        {
            return new AbsenceWriteResult<AbsenceResponse>(
                AbsenceWriteStatus.ConcurrencyConflict);
        }

        if (absence.Status == AbsenceStatus.Cancelled)
        {
            return new AbsenceWriteResult<AbsenceResponse>(
                AbsenceWriteStatus.InvalidState);
        }

        if (await HasActiveOverlapAsync(
                absence.EmployeeId,
                command.Period,
                exclusionId: absence.Id,
                cancellationToken))
        {
            return new AbsenceWriteResult<AbsenceResponse>(
                AbsenceWriteStatus.OverlapConflict);
        }

        absence.Update(
            command.Period,
            command.Type,
            command.Reason,
            command.Notes);

        dbContext.Entry(absence)
            .Property(x => x.Version)
            .OriginalValue = command.ExpectedVersion;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new AbsenceWriteResult<AbsenceResponse>(
                AbsenceWriteStatus.ConcurrencyConflict);
        }

        return new AbsenceWriteResult<AbsenceResponse>(
            AbsenceWriteStatus.Success,
            Map(absence));
    }

    public async Task<AbsenceWriteResult> CancelAsync(
        Guid id,
        CancelAbsenceCommand command,
        CancellationToken cancellationToken)
    {
        var absence = await dbContext.Absences
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (absence is null)
        {
            return new AbsenceWriteResult(AbsenceWriteStatus.NotFound);
        }

        if (absence.Version != command.ExpectedVersion)
        {
            return new AbsenceWriteResult(AbsenceWriteStatus.ConcurrencyConflict);
        }

        if (absence.Status == AbsenceStatus.Cancelled)
        {
            return new AbsenceWriteResult(AbsenceWriteStatus.Success);
        }

        absence.Cancel();

        dbContext.Entry(absence)
            .Property(x => x.Version)
            .OriginalValue = command.ExpectedVersion;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new AbsenceWriteResult(AbsenceWriteStatus.ConcurrencyConflict);
        }

        return new AbsenceWriteResult(AbsenceWriteStatus.Success);
    }

    private IQueryable<Absence> BuildQuery(AbsenceQueryFilters filters)
    {
        var query = dbContext.Absences.AsNoTracking();

        if (filters.EmployeeId.HasValue)
        {
            query = query.Where(x => x.EmployeeId == filters.EmployeeId.Value);
        }

        if (filters.Status.HasValue)
        {
            query = query.Where(x => x.Status == filters.Status.Value);
        }

        if (filters.Type.HasValue)
        {
            query = query.Where(x => x.Type == filters.Type.Value);
        }

        if (filters.From.HasValue && filters.To.HasValue)
        {
            query = query.Where(x =>
                x.Period.Start <= filters.To.Value
                && x.Period.End >= filters.From.Value);
        }
        else if (filters.From.HasValue)
        {
            query = query.Where(x => x.Period.End >= filters.From.Value);
        }
        else if (filters.To.HasValue)
        {
            query = query.Where(x => x.Period.Start <= filters.To.Value);
        }

        return query;
    }

    private async Task<bool> HasActiveOverlapAsync(
        Guid employeeId,
        DateRange period,
        Guid? exclusionId,
        CancellationToken cancellationToken)
    {
        // App-level overlap enforcement is enough for this MVP, but a database
        // constraint would still be needed to fully close the race window.
        return await dbContext.Absences
            .AsNoTracking()
            .AnyAsync(
                x => x.EmployeeId == employeeId
                     && (!exclusionId.HasValue || x.Id != exclusionId.Value)
                     && x.Status == AbsenceStatus.Active
                     && x.Period.Start <= period.End
                     && x.Period.End >= period.Start,
                cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<Absence, AbsenceResponse>>
        MapExpression()
        => x => new AbsenceResponse(
            x.Id,
            x.EmployeeId,
            x.Period.Start,
            x.Period.End,
            x.Type.ToString(),
            x.Status.ToString(),
            x.Reason,
            x.Notes,
            x.Version);

    private static AbsenceResponse Map(Absence absence)
        => new(
            absence.Id,
            absence.EmployeeId,
            absence.Period.Start,
            absence.Period.End,
            absence.Type.ToString(),
            absence.Status.ToString(),
            absence.Reason,
            absence.Notes,
            absence.Version);
}
