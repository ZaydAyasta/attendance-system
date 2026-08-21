using System.Reflection;
using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.WorkAssignments.Application;
using Attendance.Api.Modules.WorkAssignments.Domain;
using Attendance.Api.Modules.WorkCalendar.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Attendance.Api.Tests.WorkAssignments;

public sealed class WorkAssignmentServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesActiveWorkAssignment()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        await dbContext.SaveChangesAsync();

        var service = new WorkAssignmentService(dbContext);

        var result = await service.CreateAsync(
            new CreateWorkAssignmentCommand(
                employeeId,
                new DateOnly(2026, 8, 22),
                WorkAssignmentType.WeekendWork,
                "Trabajo excepcional"),
            CancellationToken.None);

        Assert.Equal(WorkAssignmentWriteStatus.Success, result.Status);
        Assert.Equal("Active", result.Value!.Status);

        var entity = await dbContext.EmployeeWorkAssignments.SingleAsync();
        Assert.Equal(WorkAssignmentStatus.Active, entity.Status);
    }

    [Fact]
    public async Task CreateAsync_ReturnsEmployeeNotFoundWhenEmployeeDoesNotExist()
    {
        await using var dbContext = await CreateDbContextAsync();
        var service = new WorkAssignmentService(dbContext);

        var result = await service.CreateAsync(
            new CreateWorkAssignmentCommand(
                Guid.NewGuid(),
                new DateOnly(2026, 8, 22),
                WorkAssignmentType.WeekendWork,
                null),
            CancellationToken.None);

        Assert.Equal(WorkAssignmentWriteStatus.EmployeeNotFound, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task CreateAsync_ReturnsEmployeeInactiveWhenEmployeeIsInactive()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: false));
        await dbContext.SaveChangesAsync();

        var service = new WorkAssignmentService(dbContext);

        var result = await service.CreateAsync(
            new CreateWorkAssignmentCommand(
                employeeId,
                new DateOnly(2026, 8, 22),
                WorkAssignmentType.Recovery,
                null),
            CancellationToken.None);

        Assert.Equal(WorkAssignmentWriteStatus.EmployeeInactive, result.Status);
    }

    [Fact]
    public async Task CreateAsync_ReturnsDuplicateConflictWhenActiveAssignmentExists()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        dbContext.EmployeeWorkAssignments.Add(
            CreateActiveAssignment(
                employeeId,
                new DateOnly(2026, 8, 22),
                WorkAssignmentType.WeekendWork));
        await dbContext.SaveChangesAsync();

        var service = new WorkAssignmentService(dbContext);

        var result = await service.CreateAsync(
            new CreateWorkAssignmentCommand(
                employeeId,
                new DateOnly(2026, 8, 22),
                WorkAssignmentType.Recovery,
                null),
            CancellationToken.None);

        Assert.Equal(WorkAssignmentWriteStatus.DuplicateActiveAssignment, result.Status);
    }

    [Fact]
    public async Task CreateAsync_CancelledAssignmentDoesNotBlockSameDate()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        dbContext.EmployeeWorkAssignments.Add(
            CreateCancelledAssignment(
                employeeId,
                new DateOnly(2026, 8, 22),
                WorkAssignmentType.WeekendWork));
        await dbContext.SaveChangesAsync();

        var service = new WorkAssignmentService(dbContext);

        var result = await service.CreateAsync(
            new CreateWorkAssignmentCommand(
                employeeId,
                new DateOnly(2026, 8, 22),
                WorkAssignmentType.Recovery,
                null),
            CancellationToken.None);

        Assert.Equal(WorkAssignmentWriteStatus.Success, result.Status);
        Assert.Equal("Active", result.Value!.Status);
    }

    [Fact]
    public async Task CreateAsync_ReturnsHolidayConflictWhenDateIsHoliday()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        var date = new DateOnly(2026, 8, 25);
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        dbContext.WorkCalendarDays.Add(WorkCalendarDay.Create(date, DayType.Holiday, null));
        await dbContext.SaveChangesAsync();

        var service = new WorkAssignmentService(dbContext);

        var result = await service.CreateAsync(
            new CreateWorkAssignmentCommand(
                employeeId,
                date,
                WorkAssignmentType.WeekendWork,
                null),
            CancellationToken.None);

        Assert.Equal(WorkAssignmentWriteStatus.HolidayConflict, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsStatusForActiveAndCancelledAssignments()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        var active = CreateActiveAssignment(
            employeeId,
            new DateOnly(2026, 8, 22),
            WorkAssignmentType.WeekendWork);
        var cancelled = CreateCancelledAssignment(
            employeeId,
            new DateOnly(2026, 8, 23),
            WorkAssignmentType.Recovery);

        dbContext.EmployeeWorkAssignments.AddRange(active, cancelled);
        await dbContext.SaveChangesAsync();

        var service = new WorkAssignmentService(dbContext);

        Assert.Equal("Active", (await service.GetByIdAsync(active.Id, CancellationToken.None))!.Status);
        Assert.Equal("Cancelled", (await service.GetByIdAsync(cancelled.Id, CancellationToken.None))!.Status);
    }

    [Fact]
    public async Task ListAsync_FiltersByEmployee()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        var otherEmployeeId = Guid.NewGuid();

        dbContext.Employees.AddRange(
            CreateEmployee(employeeId, isActive: true),
            CreateEmployee(otherEmployeeId, isActive: true));
        dbContext.EmployeeWorkAssignments.AddRange(
            CreateActiveAssignment(employeeId, new DateOnly(2026, 8, 22), WorkAssignmentType.WeekendWork),
            CreateActiveAssignment(otherEmployeeId, new DateOnly(2026, 8, 23), WorkAssignmentType.Recovery));
        await dbContext.SaveChangesAsync();

        var service = new WorkAssignmentService(dbContext);

        var result = await service.ListAsync(
            new WorkAssignmentQueryFilters(employeeId, null, null, null, null),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(employeeId, result[0].EmployeeId);
    }

    [Fact]
    public async Task ListAsync_FiltersByRange()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        dbContext.EmployeeWorkAssignments.AddRange(
            CreateActiveAssignment(employeeId, new DateOnly(2026, 8, 20), WorkAssignmentType.WeekendWork),
            CreateActiveAssignment(employeeId, new DateOnly(2026, 8, 25), WorkAssignmentType.Recovery));
        await dbContext.SaveChangesAsync();

        var service = new WorkAssignmentService(dbContext);

        var result = await service.ListAsync(
            new WorkAssignmentQueryFilters(
                null,
                new DateOnly(2026, 8, 21),
                new DateOnly(2026, 8, 26),
                null,
                null),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(new DateOnly(2026, 8, 25), result[0].Date);
    }

    [Fact]
    public async Task ListAsync_FiltersByType()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        dbContext.EmployeeWorkAssignments.AddRange(
            CreateActiveAssignment(employeeId, new DateOnly(2026, 8, 22), WorkAssignmentType.WeekendWork),
            CreateActiveAssignment(employeeId, new DateOnly(2026, 8, 23), WorkAssignmentType.Recovery));
        await dbContext.SaveChangesAsync();

        var service = new WorkAssignmentService(dbContext);

        var result = await service.ListAsync(
            new WorkAssignmentQueryFilters(null, null, null, null, WorkAssignmentType.Recovery),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Recovery", result[0].Type);
    }

    [Fact]
    public async Task ListAsync_FiltersByStatus()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        dbContext.EmployeeWorkAssignments.AddRange(
            CreateActiveAssignment(employeeId, new DateOnly(2026, 8, 22), WorkAssignmentType.WeekendWork),
            CreateCancelledAssignment(employeeId, new DateOnly(2026, 8, 23), WorkAssignmentType.Recovery));
        await dbContext.SaveChangesAsync();

        var service = new WorkAssignmentService(dbContext);

        var result = await service.ListAsync(
            new WorkAssignmentQueryFilters(null, null, null, WorkAssignmentStatus.Cancelled, null),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Cancelled", result[0].Status);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesActiveWorkAssignment()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        var assignment = CreateActiveAssignment(
            employeeId,
            new DateOnly(2026, 8, 22),
            WorkAssignmentType.WeekendWork,
            "Inicial");
        dbContext.EmployeeWorkAssignments.Add(assignment);
        await dbContext.SaveChangesAsync();
        await SetVersionAsync(dbContext, assignment.Id, 7u);
        dbContext.ChangeTracker.Clear();

        var service = new WorkAssignmentService(dbContext);

        var result = await service.UpdateAsync(
            assignment.Id,
            new UpdateWorkAssignmentCommand(
                new DateOnly(2026, 8, 23),
                WorkAssignmentType.Recovery,
                "Cambio",
                7u),
            CancellationToken.None);

        Assert.Equal(WorkAssignmentWriteStatus.Success, result.Status);
        Assert.Equal("Recovery", result.Value!.Type);
        Assert.Equal("Active", result.Value.Status);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsInvalidStateWhenAssignmentIsCancelled()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        var assignment = CreateCancelledAssignment(
            employeeId,
            new DateOnly(2026, 8, 22),
            WorkAssignmentType.WeekendWork);
        dbContext.EmployeeWorkAssignments.Add(assignment);
        await dbContext.SaveChangesAsync();
        await SetVersionAsync(dbContext, assignment.Id, 8u);
        dbContext.ChangeTracker.Clear();

        var service = new WorkAssignmentService(dbContext);

        var result = await service.UpdateAsync(
            assignment.Id,
            new UpdateWorkAssignmentCommand(
                new DateOnly(2026, 8, 23),
                WorkAssignmentType.Recovery,
                null,
                8u),
            CancellationToken.None);

        Assert.Equal(WorkAssignmentWriteStatus.InvalidState, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConcurrencyConflictWhenVersionIsStale()
    {
        await using var setupContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        setupContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        var assignment = CreateActiveAssignment(
            employeeId,
            new DateOnly(2026, 8, 22),
            WorkAssignmentType.WeekendWork);
        setupContext.EmployeeWorkAssignments.Add(assignment);
        await setupContext.SaveChangesAsync();
        await SetVersionAsync(setupContext, assignment.Id, 11u);
        setupContext.ChangeTracker.Clear();

        var service = new WorkAssignmentService(setupContext);

        var result = await service.UpdateAsync(
            assignment.Id,
            new UpdateWorkAssignmentCommand(
                new DateOnly(2026, 8, 23),
                WorkAssignmentType.Recovery,
                null,
                10u),
            CancellationToken.None);

        Assert.Equal(WorkAssignmentWriteStatus.ConcurrencyConflict, result.Status);
    }

    [Fact]
    public async Task CancelAsync_CancelsAssignment()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        var assignment = CreateActiveAssignment(
            employeeId,
            new DateOnly(2026, 8, 22),
            WorkAssignmentType.WeekendWork);
        dbContext.EmployeeWorkAssignments.Add(assignment);
        await dbContext.SaveChangesAsync();
        await SetVersionAsync(dbContext, assignment.Id, 13u);
        dbContext.ChangeTracker.Clear();

        var service = new WorkAssignmentService(dbContext);

        var result = await service.CancelAsync(
            assignment.Id,
            new CancelWorkAssignmentCommand(13u),
            CancellationToken.None);

        Assert.Equal(WorkAssignmentWriteStatus.Success, result.Status);
        var persisted = await dbContext.EmployeeWorkAssignments.SingleAsync(x => x.Id == assignment.Id);
        Assert.Equal(WorkAssignmentStatus.Cancelled, persisted.Status);
    }

    [Fact]
    public async Task CancelAsync_ReturnsConcurrencyConflictWhenVersionIsStale()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        var assignment = CreateActiveAssignment(
            employeeId,
            new DateOnly(2026, 8, 22),
            WorkAssignmentType.WeekendWork);
        dbContext.EmployeeWorkAssignments.Add(assignment);
        await dbContext.SaveChangesAsync();
        await SetVersionAsync(dbContext, assignment.Id, 14u);
        dbContext.ChangeTracker.Clear();

        var service = new WorkAssignmentService(dbContext);

        var result = await service.CancelAsync(
            assignment.Id,
            new CancelWorkAssignmentCommand(13u),
            CancellationToken.None);

        Assert.Equal(WorkAssignmentWriteStatus.ConcurrencyConflict, result.Status);
    }

    [Fact]
    public async Task GetEmployeeHistoryAsync_ReturnsEmployeeHistory()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        dbContext.EmployeeWorkAssignments.AddRange(
            CreateCancelledAssignment(employeeId, new DateOnly(2026, 8, 22), WorkAssignmentType.WeekendWork),
            CreateActiveAssignment(employeeId, new DateOnly(2026, 8, 23), WorkAssignmentType.Recovery));
        await dbContext.SaveChangesAsync();

        var service = new WorkAssignmentService(dbContext);

        var result = await service.GetEmployeeHistoryAsync(employeeId, CancellationToken.None);

        Assert.Equal(WorkAssignmentEmployeeHistoryStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task GetEmployeeHistoryAsync_ReturnsEmployeeNotFound()
    {
        await using var dbContext = await CreateDbContextAsync();
        var service = new WorkAssignmentService(dbContext);

        var result = await service.GetEmployeeHistoryAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(WorkAssignmentEmployeeHistoryStatus.EmployeeNotFound, result.Status);
    }

    private static async Task<AttendanceDbContext> CreateDbContextAsync()
    {
        var dbContext = CreateDbContext();
        await dbContext.Database.OpenConnectionAsync();
        await InitializeSchemaAsync(dbContext);

        return dbContext;
    }

    private static AttendanceDbContext CreateDbContext(SqliteConnection? connection = null)
    {
        connection ??= new SqliteConnection("Data Source=:memory:");

        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseSqlite(connection)
            .Options;

        return new AttendanceDbContext(options);
    }

    private static async Task InitializeSchemaAsync(AttendanceDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE employees (
                id TEXT NOT NULL CONSTRAINT PK_employees PRIMARY KEY,
                employee_code TEXT NOT NULL,
                first_name TEXT NOT NULL,
                last_name TEXT NOT NULL,
                is_active INTEGER NOT NULL,
                hire_date TEXT NOT NULL,
                termination_date TEXT NULL
            );
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IX_employees_employee_code
            ON employees (employee_code);
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE work_calendar_days (
                id TEXT NOT NULL CONSTRAINT PK_work_calendar_days PRIMARY KEY,
                date TEXT NOT NULL,
                day_type TEXT NOT NULL,
                description TEXT NULL,
                Version INTEGER NOT NULL DEFAULT 1
            );
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IX_work_calendar_days_date
            ON work_calendar_days (date);
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE employee_work_assignments (
                id TEXT NOT NULL CONSTRAINT PK_employee_work_assignments PRIMARY KEY,
                employee_id TEXT NOT NULL,
                date TEXT NOT NULL,
                assignment_type TEXT NOT NULL,
                comment TEXT NULL,
                status TEXT NOT NULL,
                Version INTEGER NOT NULL DEFAULT 1,
                CONSTRAINT FK_employee_work_assignments_employees_employee_id
                    FOREIGN KEY (employee_id) REFERENCES employees (id) ON DELETE RESTRICT
            );
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IX_employee_work_assignments_employee_id
            ON employee_work_assignments (employee_id);
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IX_employee_work_assignments_date
            ON employee_work_assignments (date);
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IX_employee_work_assignments_employee_id_date
            ON employee_work_assignments (employee_id, date);
            """);
    }

    private static Employee CreateEmployee(Guid id, bool isActive)
    {
        var employee = new Employee();

        SetProperty(employee, nameof(Employee.Id), id);
        SetProperty(employee, nameof(Employee.EmployeeCode), $"EMP-{id:N}"[..12]);
        SetProperty(employee, nameof(Employee.FirstName), "Ana");
        SetProperty(employee, nameof(Employee.LastName), "Perez");
        SetProperty(employee, nameof(Employee.IsActive), isActive);
        SetProperty(employee, nameof(Employee.HireDate), new DateOnly(2024, 1, 10));
        SetProperty(employee, nameof(Employee.TerminationDate), null as DateOnly?);

        return employee;
    }

    private static EmployeeWorkAssignment CreateActiveAssignment(
        Guid employeeId,
        DateOnly date,
        WorkAssignmentType type,
        string? comment = null)
        => EmployeeWorkAssignment.Create(employeeId, date, type, comment);

    private static EmployeeWorkAssignment CreateCancelledAssignment(
        Guid employeeId,
        DateOnly date,
        WorkAssignmentType type)
    {
        var assignment = EmployeeWorkAssignment.Create(employeeId, date, type, null);
        assignment.Cancel();
        return assignment;
    }

    private static async Task SetVersionAsync(
        AttendanceDbContext dbContext,
        Guid assignmentId,
        uint version)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE employee_work_assignments SET Version = {version} WHERE id = {assignmentId}");
    }

    private static void SetProperty<T>(
        Employee employee,
        string propertyName,
        T value)
        => typeof(Employee)
            .GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(employee, value);
}
