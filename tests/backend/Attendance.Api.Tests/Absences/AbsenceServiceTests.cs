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
    public async Task CreateAsync_CreatesActiveAbsence()
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
                "Vacaciones programadas",
                "Opcional"),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal("Active", result.Value!.Status);

        var entity = await dbContext.Absences.SingleAsync();
        Assert.Equal(AbsenceStatus.Active, entity.Status);
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
                null,
                null),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.EmployeeInactive, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task CreateAsync_ReturnsOverlapConflictWhenActiveAbsenceOverlaps()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        dbContext.Absences.Add(
            CreateActiveAbsence(
                employeeId,
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 15),
                AbsenceType.Vacation));
        await dbContext.SaveChangesAsync();

        var service = new AbsenceService(dbContext);

        var result = await service.CreateAsync(
            new CreateAbsenceCommand(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 14),
                    new DateOnly(2026, 8, 20)),
                AbsenceType.Commission,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.OverlapConflict, result.Status);
    }

    [Fact]
    public async Task CreateAsync_CancelledAbsenceDoesNotBlockOverlap()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));
        dbContext.Absences.Add(
            CreateCancelledAbsence(
                employeeId,
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 15),
                AbsenceType.Vacation));
        await dbContext.SaveChangesAsync();

        var service = new AbsenceService(dbContext);

        var result = await service.CreateAsync(
            new CreateAbsenceCommand(
                employeeId,
                new DateRange(
                    new DateOnly(2026, 8, 14),
                    new DateOnly(2026, 8, 20)),
                AbsenceType.Permission,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal("Active", result.Value!.Status);
    }

    [Fact]
    public async Task UpdateAsync_PreservesActiveStatus()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        var absence = CreateActiveAbsence(
            employeeId,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 15),
            AbsenceType.Vacation,
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
                "Cambio de texto",
                null,
                15u),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.Success, result.Status);
        Assert.Equal("Active", result.Value!.Status);

        var persisted = await dbContext.Absences.SingleAsync(x => x.Id == absence.Id);
        Assert.Equal(AbsenceStatus.Active, persisted.Status);
    }

    [Fact]
    public async Task UpdateAsync_ExcludesOwnAbsenceFromOverlapCheck()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        var absence = CreateActiveAbsence(
            employeeId,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 15),
            AbsenceType.Vacation,
            "Vacaciones",
            null);

        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();
        await SetVersionAsync(dbContext, absence.Id, 16u);
        dbContext.ChangeTracker.Clear();

        var service = new AbsenceService(dbContext);

        var result = await service.UpdateAsync(
            absence.Id,
            new UpdateAbsenceCommand(
                new DateRange(
                    new DateOnly(2026, 8, 10),
                    new DateOnly(2026, 8, 15)),
                AbsenceType.Vacation,
                "Cambio de texto",
                null,
                16u),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.Success, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsOverlapConflictWhenAnotherActiveAbsenceOverlaps()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        var absenceToUpdate = CreateActiveAbsence(
            employeeId,
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 5),
            AbsenceType.Permission);

        var existingAbsence = CreateActiveAbsence(
            employeeId,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 15),
            AbsenceType.Vacation);

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
                null,
                null,
                8u),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.OverlapConflict, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsInvalidStateWhenAbsenceIsCancelled()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        var absence = CreateCancelledAbsence(
            employeeId,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 15),
            AbsenceType.Vacation);

        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();
        await SetVersionAsync(dbContext, absence.Id, 17u);
        dbContext.ChangeTracker.Clear();

        var service = new AbsenceService(dbContext);

        var result = await service.UpdateAsync(
            absence.Id,
            new UpdateAbsenceCommand(
                new DateRange(
                    new DateOnly(2026, 8, 11),
                    new DateOnly(2026, 8, 16)),
                AbsenceType.Vacation,
                "Cambio",
                null,
                17u),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.InvalidState, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsStatusForActiveAndCancelledAbsences()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        var activeAbsence = CreateActiveAbsence(
            employeeId,
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 2),
            AbsenceType.Permission);
        var cancelledAbsence = CreateCancelledAbsence(
            employeeId,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 11),
            AbsenceType.Vacation);

        dbContext.Absences.AddRange(activeAbsence, cancelledAbsence);
        await dbContext.SaveChangesAsync();

        var service = new AbsenceService(dbContext);

        var activeResult = await service.GetByIdAsync(activeAbsence.Id, CancellationToken.None);
        var cancelledResult = await service.GetByIdAsync(cancelledAbsence.Id, CancellationToken.None);

        Assert.Equal("Active", activeResult!.Status);
        Assert.Equal("Cancelled", cancelledResult!.Status);
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
            CreateActiveAbsence(
                employeeId,
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 15),
                AbsenceType.Vacation),
            CreateActiveAbsence(
                otherEmployeeId,
                new DateOnly(2026, 8, 20),
                new DateOnly(2026, 8, 25),
                AbsenceType.Permission));
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
            CreateActiveAbsence(
                employeeId,
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 15),
                AbsenceType.Vacation),
            CreateActiveAbsence(
                employeeId,
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 8, 5),
                AbsenceType.Permission));
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
    public async Task ListAsync_FiltersByStatusActive()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        dbContext.Absences.AddRange(
            CreateActiveAbsence(
                employeeId,
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 8, 2),
                AbsenceType.Permission),
            CreateCancelledAbsence(
                employeeId,
                new DateOnly(2026, 8, 3),
                new DateOnly(2026, 8, 4),
                AbsenceType.Vacation));
        await dbContext.SaveChangesAsync();

        var service = new AbsenceService(dbContext);

        var result = await service.ListAsync(
            new AbsenceQueryFilters(
                null,
                null,
                null,
                AbsenceStatus.Active,
                null),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Active", result[0].Status);
    }

    [Fact]
    public async Task ListAsync_FiltersByStatusCancelled()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        dbContext.Absences.AddRange(
            CreateActiveAbsence(
                employeeId,
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 8, 2),
                AbsenceType.Permission),
            CreateCancelledAbsence(
                employeeId,
                new DateOnly(2026, 8, 3),
                new DateOnly(2026, 8, 4),
                AbsenceType.Vacation));
        await dbContext.SaveChangesAsync();

        var service = new AbsenceService(dbContext);

        var result = await service.ListAsync(
            new AbsenceQueryFilters(
                null,
                null,
                null,
                AbsenceStatus.Cancelled,
                null),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Cancelled", result[0].Status);
    }

    [Fact]
    public async Task GetEmployeeHistoryAsync_ReturnsEmployeeHistory()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        dbContext.Absences.AddRange(
            CreateCancelledAbsence(
                employeeId,
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 8, 3),
                AbsenceType.Permission),
            CreateActiveAbsence(
                employeeId,
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 15),
                AbsenceType.Vacation));
        await dbContext.SaveChangesAsync();

        var service = new AbsenceService(dbContext);

        var result = await service.GetEmployeeHistoryAsync(
            employeeId,
            CancellationToken.None);

        Assert.Equal(AbsenceEmployeeHistoryStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal("Active", result.Value[0].Status);
        Assert.Equal("Cancelled", result.Value[1].Status);
    }

    [Fact]
    public async Task CancelAsync_PerformsLogicalCancellationAndPreservesHistory()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        var absence = CreateActiveAbsence(
            employeeId,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 15),
            AbsenceType.Vacation);

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
        Assert.Equal(1, await dbContext.Absences.CountAsync());
    }

    [Fact]
    public async Task CancelAsync_IsIdempotentWhenAbsenceIsAlreadyCancelled()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        dbContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        var absence = CreateCancelledAbsence(
            employeeId,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 15),
            AbsenceType.Vacation);

        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();
        await SetVersionAsync(dbContext, absence.Id, 26u);
        dbContext.ChangeTracker.Clear();

        var service = new AbsenceService(dbContext);

        var result = await service.CancelAsync(
            absence.Id,
            new CancelAbsenceCommand(26u),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.Success, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConcurrencyConflictWhenVersionIsStale()
    {
        await using var setupContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        setupContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        var absence = CreateActiveAbsence(
            employeeId,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 15),
            AbsenceType.Vacation);

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
                "Cambio de fechas",
                null,
                10u),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.ConcurrencyConflict, result.Status);
    }

    [Fact]
    public async Task CancelAsync_ReturnsConcurrencyConflictWhenVersionIsStale()
    {
        await using var setupContext = await CreateDbContextAsync();
        var employeeId = Guid.NewGuid();
        setupContext.Employees.Add(CreateEmployee(employeeId, isActive: true));

        var absence = CreateActiveAbsence(
            employeeId,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 15),
            AbsenceType.Vacation);

        setupContext.Absences.Add(absence);
        await setupContext.SaveChangesAsync();
        await SetVersionAsync(setupContext, absence.Id, 12u);
        setupContext.ChangeTracker.Clear();

        var service = new AbsenceService(setupContext);

        var result = await service.CancelAsync(
            absence.Id,
            new CancelAbsenceCommand(11u),
            CancellationToken.None);

        Assert.Equal(AbsenceWriteStatus.ConcurrencyConflict, result.Status);
    }

    private static Absence CreateActiveAbsence(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        AbsenceType type,
        string? reason = null,
        string? notes = null)
        => Absence.Create(
            employeeId,
            new DateRange(startDate, endDate),
            type,
            reason,
            notes);

    private static Absence CreateCancelledAbsence(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        AbsenceType type,
        string? reason = null,
        string? notes = null)
    {
        var absence = CreateActiveAbsence(
            employeeId,
            startDate,
            endDate,
            type,
            reason,
            notes);

        absence.Cancel();
        return absence;
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
