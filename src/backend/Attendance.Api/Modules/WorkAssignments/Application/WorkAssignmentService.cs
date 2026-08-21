using System.Linq.Expressions;
using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.WorkAssignments.Contracts;
using Attendance.Api.Modules.WorkAssignments.Domain;
using Attendance.Api.Modules.WorkCalendar.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Attendance.Api.Modules.WorkAssignments.Application;

public sealed class WorkAssignmentService(AttendanceDbContext dbContext)
{
    private const string ActiveAssignmentPerEmployeeDateIndexName =
        "IX_employee_work_assignments_employee_id_date_active";

    public async Task<IReadOnlyList<WorkAssignmentResponse>> ListAsync(
        WorkAssignmentQueryFilters filters,
        CancellationToken cancellationToken)
        => await BuildQuery(filters)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Id)
            .Select(MapExpression())
            .ToListAsync(cancellationToken);

    public Task<WorkAssignmentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
        => dbContext.EmployeeWorkAssignments
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(MapExpression())
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<WorkAssignmentEmployeeHistoryResult> GetEmployeeHistoryAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var employeeExists = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(x => x.Id == employeeId, cancellationToken);

        if (!employeeExists)
        {
            return new WorkAssignmentEmployeeHistoryResult(
                WorkAssignmentEmployeeHistoryStatus.EmployeeNotFound);
        }

        var history = await ListAsync(
            new WorkAssignmentQueryFilters(employeeId, null, null, null, null),
            cancellationToken);

        return new WorkAssignmentEmployeeHistoryResult(
            WorkAssignmentEmployeeHistoryStatus.Success,
            history);
    }

    public async Task<WorkAssignmentWriteResult<WorkAssignmentResponse>> CreateAsync(
        CreateWorkAssignmentCommand command,
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
            return new WorkAssignmentWriteResult<WorkAssignmentResponse>(
                WorkAssignmentWriteStatus.EmployeeNotFound);
        }

        if (!employee.IsActive)
        {
            return new WorkAssignmentWriteResult<WorkAssignmentResponse>(
                WorkAssignmentWriteStatus.EmployeeInactive);
        }

        if (await IsHolidayAsync(command.Date, cancellationToken))
        {
            return new WorkAssignmentWriteResult<WorkAssignmentResponse>(
                WorkAssignmentWriteStatus.HolidayConflict);
        }

        if (await HasActiveAssignmentAsync(
                command.EmployeeId,
                command.Date,
                exclusionId: null,
                cancellationToken))
        {
            return new WorkAssignmentWriteResult<WorkAssignmentResponse>(
                WorkAssignmentWriteStatus.DuplicateActiveAssignment);
        }

        var workAssignment = EmployeeWorkAssignment.Create(
            command.EmployeeId,
            command.Date,
            command.Type,
            command.Comment);

        dbContext.EmployeeWorkAssignments.Add(workAssignment);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsDuplicateActiveAssignmentViolation(exception))
        {
            return new WorkAssignmentWriteResult<WorkAssignmentResponse>(
                WorkAssignmentWriteStatus.DuplicateActiveAssignment);
        }

        return new WorkAssignmentWriteResult<WorkAssignmentResponse>(
            WorkAssignmentWriteStatus.Success,
            Map(workAssignment));
    }

    public async Task<WorkAssignmentWriteResult<WorkAssignmentResponse>> UpdateAsync(
        Guid id,
        UpdateWorkAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        var workAssignment = await dbContext.EmployeeWorkAssignments
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (workAssignment is null)
        {
            return new WorkAssignmentWriteResult<WorkAssignmentResponse>(
                WorkAssignmentWriteStatus.NotFound);
        }

        if (workAssignment.Version != command.ExpectedVersion)
        {
            return new WorkAssignmentWriteResult<WorkAssignmentResponse>(
                WorkAssignmentWriteStatus.ConcurrencyConflict);
        }

        if (workAssignment.Status == WorkAssignmentStatus.Cancelled)
        {
            return new WorkAssignmentWriteResult<WorkAssignmentResponse>(
                WorkAssignmentWriteStatus.InvalidState);
        }

        if (await IsHolidayAsync(command.Date, cancellationToken))
        {
            return new WorkAssignmentWriteResult<WorkAssignmentResponse>(
                WorkAssignmentWriteStatus.HolidayConflict);
        }

        if (await HasActiveAssignmentAsync(
                workAssignment.EmployeeId,
                command.Date,
                workAssignment.Id,
                cancellationToken))
        {
            return new WorkAssignmentWriteResult<WorkAssignmentResponse>(
                WorkAssignmentWriteStatus.DuplicateActiveAssignment);
        }

        workAssignment.Update(
            command.Date,
            command.Type,
            command.Comment);

        dbContext.Entry(workAssignment)
            .Property(x => x.Version)
            .OriginalValue = command.ExpectedVersion;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new WorkAssignmentWriteResult<WorkAssignmentResponse>(
                WorkAssignmentWriteStatus.ConcurrencyConflict);
        }
        catch (DbUpdateException exception)
            when (IsDuplicateActiveAssignmentViolation(exception))
        {
            return new WorkAssignmentWriteResult<WorkAssignmentResponse>(
                WorkAssignmentWriteStatus.DuplicateActiveAssignment);
        }

        return new WorkAssignmentWriteResult<WorkAssignmentResponse>(
            WorkAssignmentWriteStatus.Success,
            Map(workAssignment));
    }

    public async Task<WorkAssignmentWriteResult> CancelAsync(
        Guid id,
        CancelWorkAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        var workAssignment = await dbContext.EmployeeWorkAssignments
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (workAssignment is null)
        {
            return new WorkAssignmentWriteResult(WorkAssignmentWriteStatus.NotFound);
        }

        if (workAssignment.Version != command.ExpectedVersion)
        {
            return new WorkAssignmentWriteResult(
                WorkAssignmentWriteStatus.ConcurrencyConflict);
        }

        if (workAssignment.Status == WorkAssignmentStatus.Cancelled)
        {
            return new WorkAssignmentWriteResult(WorkAssignmentWriteStatus.Success);
        }

        workAssignment.Cancel();

        dbContext.Entry(workAssignment)
            .Property(x => x.Version)
            .OriginalValue = command.ExpectedVersion;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new WorkAssignmentWriteResult(
                WorkAssignmentWriteStatus.ConcurrencyConflict);
        }

        return new WorkAssignmentWriteResult(WorkAssignmentWriteStatus.Success);
    }

    private IQueryable<EmployeeWorkAssignment> BuildQuery(WorkAssignmentQueryFilters filters)
    {
        var query = dbContext.EmployeeWorkAssignments.AsNoTracking();

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

        if (filters.From.HasValue)
        {
            query = query.Where(x => x.Date >= filters.From.Value);
        }

        if (filters.To.HasValue)
        {
            query = query.Where(x => x.Date <= filters.To.Value);
        }

        return query;
    }

    private async Task<bool> HasActiveAssignmentAsync(
        Guid employeeId,
        DateOnly date,
        Guid? exclusionId,
        CancellationToken cancellationToken)
        => await dbContext.EmployeeWorkAssignments
            .AsNoTracking()
            .AnyAsync(
                x => x.EmployeeId == employeeId
                     && x.Date == date
                     && x.Status == WorkAssignmentStatus.Active
                     && (!exclusionId.HasValue || x.Id != exclusionId.Value),
                cancellationToken);

    private async Task<bool> IsHolidayAsync(
        DateOnly date,
        CancellationToken cancellationToken)
        => await dbContext.WorkCalendarDays
            .AsNoTracking()
            .AnyAsync(
                x => x.Date == date && x.DayType == DayType.Holiday,
                cancellationToken);

    private static bool IsDuplicateActiveAssignmentViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException postgresException
           && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
           && string.Equals(
               postgresException.ConstraintName,
               ActiveAssignmentPerEmployeeDateIndexName,
               StringComparison.Ordinal);

    private static Expression<Func<EmployeeWorkAssignment, WorkAssignmentResponse>>
        MapExpression()
        => x => new WorkAssignmentResponse(
            x.Id,
            x.EmployeeId,
            x.Date,
            x.Type.ToString(),
            x.Comment,
            x.Status.ToString(),
            x.Version);

    private static WorkAssignmentResponse Map(EmployeeWorkAssignment workAssignment)
        => new(
            workAssignment.Id,
            workAssignment.EmployeeId,
            workAssignment.Date,
            workAssignment.Type.ToString(),
            workAssignment.Comment,
            workAssignment.Status.ToString(),
            workAssignment.Version);
}
