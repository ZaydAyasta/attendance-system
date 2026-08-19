using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.WorkCalendar.Contracts;
using Attendance.Api.Modules.WorkCalendar.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Attendance.Api.Modules.WorkCalendar.Application;

public sealed class WorkCalendarService(AttendanceDbContext dbContext)
{
    private const string UniqueDateIndexName = "IX_work_calendar_days_date";

    public async Task<IReadOnlyList<WorkCalendarDayResponse>> ListAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var query = dbContext.WorkCalendarDays.AsNoTracking();

        if (from.HasValue)
        {
            query = query.Where(x => x.Date >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.Date <= to.Value);
        }

        return await query
            .OrderBy(x => x.Date)
            .Select(x => new WorkCalendarDayResponse(
                x.Date,
                x.DayType.ToString(),
                x.Description,
                x.Version))
            .ToListAsync(cancellationToken);
    }

    public Task<WorkCalendarDayResponse?> GetByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken)
        => dbContext.WorkCalendarDays
            .AsNoTracking()
            .Where(x => x.Date == date)
            .Select(x => new WorkCalendarDayResponse(
                x.Date,
                x.DayType.ToString(),
                x.Description,
                x.Version))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<WorkCalendarWriteResult<WorkCalendarDayResponse>> CreateAsync(
        CreateWorkCalendarDayCommand command,
        CancellationToken cancellationToken)
    {
        var alreadyExists = await dbContext.WorkCalendarDays
            .AsNoTracking()
            .AnyAsync(x => x.Date == command.Date, cancellationToken);

        if (alreadyExists)
        {
            return new WorkCalendarWriteResult<WorkCalendarDayResponse>(
                WorkCalendarWriteStatus.Duplicate);
        }

        var workCalendarDay = WorkCalendarDay.Create(
            command.Date,
            command.DayType,
            command.Description);

        dbContext.WorkCalendarDays.Add(workCalendarDay);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueDateViolation(exception))
        {
            return new WorkCalendarWriteResult<WorkCalendarDayResponse>(
                WorkCalendarWriteStatus.Duplicate);
        }

        return new WorkCalendarWriteResult<WorkCalendarDayResponse>(
            WorkCalendarWriteStatus.Success,
            Map(workCalendarDay));
    }

    public async Task<WorkCalendarWriteResult<WorkCalendarDayResponse>> UpdateAsync(
        DateOnly date,
        UpdateWorkCalendarDayCommand command,
        CancellationToken cancellationToken)
    {
        var workCalendarDay = await dbContext.WorkCalendarDays
            .SingleOrDefaultAsync(x => x.Date == date, cancellationToken);

        if (workCalendarDay is null)
        {
            return new WorkCalendarWriteResult<WorkCalendarDayResponse>(
                WorkCalendarWriteStatus.NotFound);
        }

        workCalendarDay.Update(command.DayType, command.Description);
        dbContext.Entry(workCalendarDay)
            .Property(x => x.Version)
            .OriginalValue = command.ExpectedVersion;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new WorkCalendarWriteResult<WorkCalendarDayResponse>(
                WorkCalendarWriteStatus.ConcurrencyConflict);
        }

        return new WorkCalendarWriteResult<WorkCalendarDayResponse>(
            WorkCalendarWriteStatus.Success,
            Map(workCalendarDay));
    }

    public async Task<WorkCalendarWriteResult> DeleteAsync(
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var workCalendarDay = await dbContext.WorkCalendarDays
            .SingleOrDefaultAsync(x => x.Date == date, cancellationToken);

        if (workCalendarDay is null)
        {
            return new WorkCalendarWriteResult(WorkCalendarWriteStatus.NotFound);
        }

        dbContext.WorkCalendarDays.Remove(workCalendarDay);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new WorkCalendarWriteResult(
                WorkCalendarWriteStatus.ConcurrencyConflict);
        }

        return new WorkCalendarWriteResult(WorkCalendarWriteStatus.Success);
    }

    private static WorkCalendarDayResponse Map(WorkCalendarDay workCalendarDay)
        => new(
            workCalendarDay.Date,
            workCalendarDay.DayType.ToString(),
            workCalendarDay.Description,
            workCalendarDay.Version);

    private static bool IsUniqueDateViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException postgresException
           && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
           && string.Equals(
               postgresException.ConstraintName,
               UniqueDateIndexName,
               StringComparison.OrdinalIgnoreCase);
}
