using System.Reflection;
using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Employees.Application;
using Attendance.Api.Modules.Employees.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Attendance.Api.Tests.Employees;

public sealed class EmployeeDirectoryServiceTests
{
    [Fact]
    public async Task ListAsync_WhenActive_ReturnsOnlyActiveEmployeesWithReadableFields()
    {
        await using var db = new AttendanceDbContext(new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var activeId = Guid.NewGuid();
        db.Employees.AddRange(CreateEmployee(activeId, "EMP-001", "Juan", "Pérez", true),
            CreateEmployee(Guid.NewGuid(), "EMP-002", "Ana", "López", false));
        await db.SaveChangesAsync();

        var result = await new EmployeeDirectoryService(db).ListAsync(true, CancellationToken.None);

        var employee = Assert.Single(result);
        Assert.Equal(activeId, employee.Id);
        Assert.Equal("EMP-001", employee.EmployeeCode);
        Assert.Equal("Juan Pérez", employee.FullName);
    }

    private static Employee CreateEmployee(Guid id, string code, string firstName, string lastName, bool active)
    {
        var employee = (Employee)Activator.CreateInstance(typeof(Employee), true)!;
        Set(employee, nameof(Employee.Id), id); Set(employee, nameof(Employee.EmployeeCode), code);
        Set(employee, nameof(Employee.FirstName), firstName); Set(employee, nameof(Employee.LastName), lastName);
        Set(employee, nameof(Employee.IsActive), active); Set(employee, nameof(Employee.HireDate), new DateOnly(2024, 1, 1));
        return employee;
    }
    private static void Set<T>(Employee employee, string property, T value) => typeof(Employee).GetProperty(property)!.SetValue(employee, value);
}
