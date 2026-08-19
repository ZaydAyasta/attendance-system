using System.Reflection;
using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Absences.Application;
using Attendance.Api.Modules.Absences.Domain;
using Attendance.Api.Modules.Employees.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Attendance.Api.Tests.Absences;

public sealed class AbsenceServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesValidAbsence()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        await dbContext.SaveChangesAsync();

        var service = new AbsenceService(dbContext);

        var result = await service.CreateAsync(
            new CreateAbsenceCommand(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 10),
                    new DateOnly(2026, 8, 15)),
                AbsenceType.Vacation,
                AbsenceStatus.Approved,
                "Vacaciones programadas",
                "Opcional"),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(employeeId, result.Value!.EmployeeId);
        Assert.Equal(new DateOnly(2026, 8, 10), result.Value.StartDate);
        Assert.Equal(new DateOnly(2026, 8, 15), result.Value.EndDate);
        Assert.Equal("Vacation", result.Value.Type);
        Assert.Equal("Approved", result.Value.Status);

        var entity = await dbContext.Absences.SingleAsync();
        Assert.Equal(employeeId, entity.EmployeeId);
        Assert.Equal(AbsenceStatus.Approved, entity.Status);
    }

    [Fact]
    public async Task CreateAsync_ReturnsEmployeeNotFoundWhenEmployeeDoesNotExist()
    {
        await using var dbContext = await CreateDbContextAsync();
        var service = new AbsenceService(dbContext);

        var result = await service.CreateAsync(
            new CreateAbsenceCommand(
                Guid.NewGuid(),
                new DateRange(
                    new DateOnly(2026, 8, 10),
                    new DateOnly(2026, 8, 15)),
                AbsenceType.Vacation,
                AbsenceStatus.Approved,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.EmployeeNotFound, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task CreateAsync_ReturnsEmployeeInactiveWhenEmployeeIsInactive()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: false));
        await dbContext.SaveChangesAsync();

        var service = new AbsenceService(dbContext);

        var result = await service.CreateAsync(
            new CreateAbsenceCommand(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 10),
                    new DateOnly(2026, 8, 15)),
                AbsenceType.Permission,
                AbsenceStatus.Pending,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.EmployeeInactive, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task CreateAsync_ReturnsOverlapConflictWhenPendingAbsenceOverlaps()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        dbContext.Absences.Add(
            Absence.Create(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 10),
                    new DateOnly(2026, 8, 15)),
                AbsenceType.Vacation,
                AbsenceStatus.Pending,
                null,
                null));
        await dbContext.SaveChangesAsync();

        var service = new AbsenceService(dbContext);

        var result = await service.CreateAsync(
            new CreateAbsenceCommand(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 14),
                    new DateOnly(2026, 8, 20)),
                AbsenceType.Commission,
                AbsenceStatus.Approved,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.OverlapConflict, result.Status);
    }

    [Fact]
    public async Task CreateAsync_ReturnsOverlapConflictWhenApprovedAbsenceOverlaps()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        dbContext.Absences.Add(
            Absence.Create(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 10),
                    new DateOnly(2026, 8, 15)),
                AbsenceType.Vacation,
                AbsenceStatus.Approved,
                null,
                null));
        await dbContext.SaveChangesAsync();

        var service = new AbsenceService(dbContext);

        var result = await service.CreateAsync(
            new CreateAbsenceCommand(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 14),
                    new DateOnly(2026, 8, 20)),
                AbsenceType.Permission,
                AbsenceStatus.Pending,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.OverlapConflict, result.Status);
    }

    [Fact]
    public async Task CreateAsync_RejectedAbsenceDoesNotBlockOverlap()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        dbContext.Absences.Add(
            Absence.Create(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 10),
                    new DateOnly(2026, 8, 15)),
                AbsenceType.Vacation,
                AbsenceStatus.Rejected,
                null,
                null));
        await dbContext.SaveChangesAsync();

        var service = new AbsenceService(dbContext);

        var result = await service.CreateAsync(
            new CreateAbsenceCommand(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 14),
                    new DateOnly(2026, 8, 20)),
                AbsenceType.Permission,
                AbsenceStatus.Approved,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.Success, result.Status);
    }

    [Fact]
    public async Task CreateAsync_CancelledAbsenceDoesNotBlockOverlap()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        dbContext.Absences.Add(
            Absence.Create(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 10),
                    new DateOnly(2026, 8, 15)),
                AbsenceType.Vacation,
                AbsenceStatus.Cancelled,
                null,
                null));
        await dbContext.SaveChangesAsync();

        var service = new AbsenceService(dbContext);

        var result = await service.CreateAsync(
            new CreateAbsenceCommand(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 14),
                    new DateOnly(2026, 8, 20)),
                AbsenceType.Permission,
                AbsenceStatus.Approved,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.Success, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_ExcludesOwnAbsenceFromOverlapCheck()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        var absence = Absence.Create(
            employeeId,
            new DateRange(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 15)),
            AbsenceType.Vacation,
            AbsenceStatus.Approved,
            "Vacaciones",
            null);

        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();
        await SetVersionAsync(dbContext, absence.Id, 15u);
        dbContext.ChangeTracker.Clear();

        var service = new AbsenceService(dbContext);

        var result = await service.UpdateAsync(
            absence.Id,
            new UpdateAbsenceCommand(
                new DateRange(
                    new DateOnly(2026, 8, 10),
                    new DateOnly(2026, 8, 15)),
                AbsenceType.Vacation,
                AbsenceStatus.Approved,
                "Cambio de texto",
                null,
                15u),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.Success, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsOverlapConflictWhenAnotherActiveAbsenceOverlaps()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        var absenceToUpdate = Absence.Create(
            employeeId,
            new DateRange(
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 8, 5)),
            AbsenceType.Permission,
            AbsenceStatus.Approved,
            null,
            null);

        var existingAbsence = Absence.Create(
            employeeId,
            new DateRange(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 15)),
            AbsenceType.Vacation,
            AbsenceStatus.Approved,
            null,
            null);

        dbContext.Absences.AddRange(absenceToUpdate, existingAbsence);
        await dbContext.SaveChangesAsync();
        await SetVersionAsync(dbContext, absenceToUpdate.Id, 8u);
        dbContext.ChangeTracker.Clear();

        var service = new AbsenceService(dbContext);

        var result = await service.UpdateAsync(
            absenceToUpdate.Id,
            new UpdateAbsenceCommand(
                new DateRange(
                    new DateOnly(2026, 8, 14),
                    new DateOnly(2026, 8, 20)),
                AbsenceType.Permission,
                AbsenceStatus.Approved,
                null,
                null,
                8u),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.OverlapConflict, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_RejectedResultDoesNotBlockOverlap()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        var absenceToUpdate = Absence.Create(
            employeeId,
            new DateRange(
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 8, 5)),
            AbsenceType.Permission,
            AbsenceStatus.Pending,
            null,
            null);

        var existingAbsence = Absence.Create(
            employeeId,
            new DateRange(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 15)),
            AbsenceType.Vacation,
            AbsenceStatus.Approved,
            null,
            null);

        dbContext.Absences.AddRange(absenceToUpdate, existingAbsence);
        await dbContext.SaveChangesAsync();
        await SetVersionAsync(dbContext, absenceToUpdate.Id, 18u);
        dbContext.ChangeTracker.Clear();

        var service = new AbsenceService(dbContext);

        var result = await service.UpdateAsync(
            absenceToUpdate.Id,
            new UpdateAbsenceCommand(
                new DateRange(
                    new DateOnly(2026, 8, 14),
                    new DateOnly(2026, 8, 20)),
                AbsenceType.Permission,
                AbsenceStatus.Rejected,
                null,
                null,
                18u),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.Success, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_CancelledResultDoesNotBlockOverlap()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        var absenceToUpdate = Absence.Create(
            employeeId,
            new DateRange(
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 8, 5)),
            AbsenceType.Permission,
            AbsenceStatus.Pending,
            null,
            null);

        var existingAbsence = Absence.Create(
            employeeId,
            new DateRange(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 15)),
            AbsenceType.Vacation,
            AbsenceStatus.Approved,
            null,
            null);

        dbContext.Absences.AddRange(absenceToUpdate, existingAbsence);
        await dbContext.SaveChangesAsync();
        await SetVersionAsync(dbContext, absenceToUpdate.Id, 19u);
        dbContext.ChangeTracker.Clear();

        var service = new AbsenceService(dbContext);

        var result = await service.UpdateAsync(
            absenceToUpdate.Id,
            new UpdateAbsenceCommand(
                new DateRange(
                    new DateOnly(2026, 8, 14),
                    new DateOnly(2026, 8, 20)),
                AbsenceType.Permission,
                AbsenceStatus.Cancelled,
                null,
                null,
                19u),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.Success, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenAbsenceDoesNotExist()
    {
        await using var dbContext = await CreateDbContextAsync();
        var service = new AbsenceService(dbContext);

        var result = await service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
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

        dbContext.Absences.AddRange(
            Absence.Create(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 10),
                    new DateOnly(2026, 8, 15)),
                AbsenceType.Vacation,
                AbsenceStatus.Approved,
                null,
                null),
            Absence.Create(
                otherEmployeeId,
                new DateRange(
                    new DateOnly(2026, 8, 20),
                    new DateOnly(2026, 8, 25)),
                AbsenceType.Permission,
                AbsenceStatus.Pending,
                null,
                null));
        await dbContext.SaveChangesAsync();

        var service = new AbsenceService(dbContext);

        var result = await service.ListAsync(
            new AbsenceQueryFilters(employeeId, null, null, null, null),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(employeeId, result[0].EmployeeId);
    }

    [Fact]
    public async Task ListAsync_FiltersByRangeUsingIntersection()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        dbContext.Absences.AddRange(
            Absence.Create(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 10),
                    new DateOnly(2026, 8, 15)),
                AbsenceType.Vacation,
                AbsenceStatus.Approved,
                null,
                null),
            Absence.Create(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 1),
                    new DateOnly(2026, 8, 5)),
                AbsenceType.Permission,
                AbsenceStatus.Approved,
                null,
                null));
        await dbContext.SaveChangesAsync();

        var service = new AbsenceService(dbContext);

        var result = await service.ListAsync(
            new AbsenceQueryFilters(
                null,
                new DateOnly(2026, 8, 14),
                new DateOnly(2026, 8, 20),
                null,
                null),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(new DateOnly(2026, 8, 10), result[0].StartDate);
    }

    [Fact]
    public async Task GetEmployeeHistoryAsync_ReturnsEmployeeHistory()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        dbContext.Absences.AddRange(
            Absence.Create(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 1),
                    new DateOnly(2026, 8, 3)),
                AbsenceType.Permission,
                AbsenceStatus.Rejected,
                null,
                null),
            Absence.Create(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 10),
                    new DateOnly(2026, 8, 15)),
                AbsenceType.Vacation,
                AbsenceStatus.Approved,
                null,
                null));
        await dbContext.SaveChangesAsync();

        var service = new AbsenceService(dbContext);

        var result = await service.GetEmployeeHistoryAsync(
            employeeId,
            CancellationToken.None);

        Assert.Equal(AbsenceEmployeeHistoryStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal(new DateOnly(2026, 8, 10), result.Value[0].StartDate);
    }

    [Fact]
    public async Task CancelAsync_PerformsLogicalCancellation()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        var absence = Absence.Create(
            employeeId,
            new DateRange(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 15)),
            AbsenceType.Vacation,
            AbsenceStatus.Approved,
            null,
            null);

        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();
        await SetVersionAsync(dbContext, absence.Id, 25u);
        dbContext.ChangeTracker.Clear();

        var service = new AbsenceService(dbContext);

        var result = await service.CancelAsync(
            absence.Id,
            new CancelAbsenceCommand(25u),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.Success, result.Status);

        var persisted = await dbContext.Absences.SingleAsync(x => x.Id == absence.Id);
        Assert.Equal(AbsenceStatus.Cancelled, persisted.Status);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConcurrencyConflictWhenVersionIsStale()
    {
        await using var setupContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        setupContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        var absence = Absence.Create(
            employeeId,
            new DateRange(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 15)),
            AbsenceType.Vacation,
            AbsenceStatus.Approved,
            null,
            null);

        setupContext.Absences.Add(absence);
        await setupContext.SaveChangesAsync();
        await SetVersionAsync(setupContext, absence.Id, 11u);
        setupContext.ChangeTracker.Clear();

        var service = new AbsenceService(setupContext);

        var result = await service.UpdateAsync(
            absence.Id,
            new UpdateAbsenceCommand(
                new DateRange(
                    new DateOnly(2026, 8, 11),
                    new DateOnly(2026, 8, 16)),
                AbsenceType.Vacation,
                AbsenceStatus.Approved,
                "Cambio de fechas",
                null,
                10u),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.ConcurrencyConflict, result.Status);
    }

    private static async Task<AttendanceDbContext> CreateDbContextAsync()
    {
        var dbContext = CreateDbContext();
        await dbContext.Database.OpenConnectionAsync();
        await InitializeSchemaAsync(dbContext);

        return dbContext;
    }

    private static AttendanceDbContext CreateDbContext(SqliteConnection connection)
    {
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
            CREATE TABLE absences (
                id TEXT NOT NULL CONSTRAINT PK_absences PRIMARY KEY,
                employee_id TEXT NOT NULL,
                absence_type TEXT NOT NULL,
                status TEXT NOT NULL,
                reason TEXT NULL,
                notes TEXT NULL,
                Version INTEGER NOT NULL DEFAULT 1,
                start_date TEXT NOT NULL,
                end_date TEXT NOT NULL,
                CONSTRAINT FK_absences_employees_employee_id
                    FOREIGN KEY (employee_id) REFERENCES employees (id) ON DELETE RESTRICT
            );
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IX_absences_employee_id
            ON absences (employee_id);
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
        SetProperty(
            employee,
            nameof(Employee.TerminationDate),
            isActive ? null : new DateOnly?(new DateOnly(2026, 1, 1)));

        return employee;
    }

    private static async Task SetVersionAsync(
        AttendanceDbContext dbContext,
        Guid absenceId,
        uint version)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE absences SET Version = {version} WHERE id = {absenceId}");
    }

    private static void SetProperty<T>(
        Employee employee,
        string propertyName,
        T value)
    {
        typeof(Employee)
            .GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(employee, value);
    }

    private static AttendanceDbContext CreateDbContext()
        => CreateDbContext(new SqliteConnection("Data Source=:memory:"));
}
