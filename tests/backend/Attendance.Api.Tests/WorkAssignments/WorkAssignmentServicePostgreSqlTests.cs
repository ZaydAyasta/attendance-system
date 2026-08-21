using System.Reflection;
using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.WorkAssignments.Domain;
using Attendance.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Attendance.Api.Tests.WorkAssignments;

public sealed class WorkAssignmentServicePostgreSqlTests
    : IClassFixture<PostgreSqlAttendanceDatabaseFixture>
{
    private readonly PostgreSqlAttendanceDatabaseFixture fixture;

    public WorkAssignmentServicePostgreSqlTests(
        PostgreSqlAttendanceDatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    [RequiresContainerRuntimeFact]
    public async Task SaveChangesAsync_RejectsTwoActiveAssignmentsForSameEmployeeAndDate()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 22);

        dbContext.Employees.Add(employee);
        dbContext.EmployeeWorkAssignments.AddRange(
            EmployeeWorkAssignment.Create(
                employee.Id,
                date,
                WorkAssignmentType.WeekendWork,
                null),
            EmployeeWorkAssignment.Create(
                employee.Id,
                date,
                WorkAssignmentType.Recovery,
                null));

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [RequiresContainerRuntimeFact]
    public async Task SaveChangesAsync_AllowsCancelledAndNewActiveAssignmentForSameEmployeeAndDate()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 22);
        var cancelled = EmployeeWorkAssignment.Create(
            employee.Id,
            date,
            WorkAssignmentType.WeekendWork,
            null);
        cancelled.Cancel();

        dbContext.Employees.Add(employee);
        dbContext.EmployeeWorkAssignments.AddRange(
            cancelled,
            EmployeeWorkAssignment.Create(
                employee.Id,
                date,
                WorkAssignmentType.Recovery,
                null));

        await dbContext.SaveChangesAsync();

        Assert.Equal(2, await dbContext.EmployeeWorkAssignments.CountAsync());
    }

    private async Task<AttendanceDbContext> CreateDbContextAsync()
    {
        fixture.ThrowIfUnavailable();
        await fixture.ResetAsync();

        return fixture.CreateDbContext();
    }

    private static Employee CreateEmployee()
    {
        var employee = new Employee();
        var id = Guid.NewGuid();

        SetEmployeeProperty(employee, nameof(Employee.Id), id);
        SetEmployeeProperty(employee, nameof(Employee.EmployeeCode), $"EMP-{id:N}"[..12]);
        SetEmployeeProperty(employee, nameof(Employee.FirstName), "Ana");
        SetEmployeeProperty(employee, nameof(Employee.LastName), "Perez");
        SetEmployeeProperty(employee, nameof(Employee.IsActive), true);
        SetEmployeeProperty(employee, nameof(Employee.HireDate), new DateOnly(2024, 1, 10));
        SetEmployeeProperty(employee, nameof(Employee.TerminationDate), null as DateOnly?);

        return employee;
    }

    private static void SetEmployeeProperty<T>(
        Employee employee,
        string propertyName,
        T value)
        => typeof(Employee)
            .GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(employee, value);
}
