using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.WorkCalendar.Contracts;
using Attendance.Api.Modules.WorkCalendar.Domain;
using Attendance.Api.Modules.Auditing.Application;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Attendance.Api.Modules.WorkCalendar.Application;

public sealed class WorkCalendarService(AttendanceDbContext dbContext, AuditOperationContext auditContext)
{
    private const string UniqueDateIndexName = "IX_work_calendar_days_date";

    public WorkCalendarService(AttendanceDbContext dbContext) : this(dbContext, new AuditOperationContext()) { }

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

    public async Task<WorkCalendarWriteResult<BulkConfigureWorkCalendarResponse>> BulkConfigureAsync(
        BulkConfigureWorkCalendarCommand command,
        CancellationToken cancellationToken)
    {
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var dates = command.Days.Select(x => x.Date).ToList();
            var existingByDate = await dbContext.WorkCalendarDays
                .Where(x => dates.Contains(x.Date))
                .ToDictionaryAsync(x => x.Date, cancellationToken);
            var created = 0;
            var updated = 0;
            var skipped = 0;

            foreach (var day in command.Days)
            {
                if (!existingByDate.TryGetValue(day.Date, out var existing))
                {
                    dbContext.WorkCalendarDays.Add(WorkCalendarDay.Create(day.Date, day.DayType, day.Description));
                    created++;
                    continue;
                }

                if (!command.OverwriteExisting)
                {
                    skipped++;
                    continue;
                }

                // An overwrite must include the xmin that was read by the client.
                if (!day.ExpectedVersion.HasValue)
                {
                    await RollbackAsync(transaction, cancellationToken);
                    return new WorkCalendarWriteResult<BulkConfigureWorkCalendarResponse>(
                        WorkCalendarWriteStatus.ConcurrencyConflict);
                }

                existing.Update(day.DayType, day.Description);
                dbContext.Entry(existing).Property(x => x.Version).OriginalValue = day.ExpectedVersion.Value;
                updated++;
            }

            auditContext.UpdateMetadata(new
            {
                from = dates.Min(),
                to = dates.Max(),
                created,
                updated,
                skipped,
                command.OverwriteExisting
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return new WorkCalendarWriteResult<BulkConfigureWorkCalendarResponse>(
                WorkCalendarWriteStatus.Success,
                new BulkConfigureWorkCalendarResponse(created, updated, skipped));
        }
        catch (DbUpdateConcurrencyException)
        {
            await RollbackAsync(transaction, cancellationToken);
            return new WorkCalendarWriteResult<BulkConfigureWorkCalendarResponse>(
                WorkCalendarWriteStatus.ConcurrencyConflict);
        }
        catch
        {
            await RollbackAsync(transaction, cancellationToken);
            throw;
        }
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

    private static Task RollbackAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction,
        CancellationToken cancellationToken)
        => transaction is null ? Task.CompletedTask : transaction.RollbackAsync(cancellationToken);
}
