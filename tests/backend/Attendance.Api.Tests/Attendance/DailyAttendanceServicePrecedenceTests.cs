using System.Reflection;
using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Absences.Domain;
using Attendance.Api.Modules.Attendance.Application;
using Attendance.Api.Modules.Attendance.Domain;
using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.WorkCalendar.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Attendance.Api.Tests.Attendance;

public sealed class DailyAttendanceServicePrecedenceTests
{
    private static readonly AttendanceTimeZone AttendanceTimeZone =
        new("America/Lima");

    [Fact]
    public void EvaluateDate_DateAfterTerminationDateWithMultipleActiveAbsences_ReturnsNotApplicable()
    {
        var date = new DateOnly(2026, 8, 20);
        var employee = CreateEmployee(terminationDate: new DateOnly(2026, 8, 19));
        var result = EvaluateDate(
            employee,
            date,
            CreateWorkCalendarDay(date, DayType.WorkingDay),
            [
                CreateAbsence(employee.Id, date, date, AbsenceType.Vacation),
                CreateAbsence(employee.Id, date, date, AbsenceType.Permission)
            ],
            Array.Empty<AttendanceMark>());

        Assert.Equal(AttendanceStatus.NotApplicable, result.Status);
        Assert.Null(result.Failure);
    }

    [Fact]
    public void EvaluateDate_DateBeforeHireDateWithMultipleActiveAbsences_ReturnsNotApplicable()
    {
        var date = new DateOnly(2026, 8, 20);
        var employee = CreateEmployee(hireDate: new DateOnly(2026, 8, 21));
        var result = EvaluateDate(
            employee,
            date,
            CreateWorkCalendarDay(date, DayType.WorkingDay),
            [
                CreateAbsence(employee.Id, date, date, AbsenceType.Vacation),
                CreateAbsence(employee.Id, date, date, AbsenceType.Permission)
            ],
            Array.Empty<AttendanceMark>());

        Assert.Equal(AttendanceStatus.NotApplicable, result.Status);
        Assert.Null(result.Failure);
    }

    [Fact]
    public void EvaluateDate_MissingCalendarWithMultipleActiveAbsences_ReturnsMissingWorkCalendarDay()
    {
        var date = new DateOnly(2026, 8, 20);
        var employee = CreateEmployee();
        var result = EvaluateDate(
            employee,
            date,
            null,
            [
                CreateAbsence(employee.Id, date, date, AbsenceType.Vacation),
                CreateAbsence(employee.Id, date, date, AbsenceType.Permission)
            ],
            Array.Empty<AttendanceMark>());

        Assert.Null(result.Status);
        Assert.Equal(AttendanceEvaluationFailure.MissingWorkCalendarDay, result.Failure);
    }

    [Fact]
    public void EvaluateDate_ApplicableDateWithCalendarAndMultipleActiveAbsences_ReturnsMultipleActiveAbsences()
    {
        var date = new DateOnly(2026, 8, 20);
        var employee = CreateEmployee();
        var result = EvaluateDate(
            employee,
            date,
            CreateWorkCalendarDay(date, DayType.WorkingDay),
            [
                CreateAbsence(employee.Id, date, date, AbsenceType.Vacation),
                CreateAbsence(employee.Id, date, date, AbsenceType.Permission)
            ],
            Array.Empty<AttendanceMark>());

        Assert.Null(result.Status);
        Assert.Equal(AttendanceEvaluationFailure.MultipleActiveAbsences, result.Failure);
    }

    private static DailyAttendanceResult EvaluateDate(
        Employee employee,
        DateOnly date,
        WorkCalendarDay? workCalendarDay,
        IReadOnlyCollection<Absence> absences,
        IReadOnlyCollection<AttendanceMark> marks)
    {
        using var dbContext = CreateDbContext();
        var service = new DailyAttendanceService(
            dbContext,
            new AttendanceEvaluator(),
            new AttendanceTimeCalculator(),
            AttendanceTimeZone);

        return (DailyAttendanceResult)typeof(DailyAttendanceService)
            .GetMethod(
                "EvaluateDate",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(service, [employee, date, workCalendarDay, absences, marks])!;
    }

    private static AttendanceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AttendanceDbContext(options);
    }

    private static WorkCalendarDay CreateWorkCalendarDay(
        DateOnly date,
        DayType dayType)
        => WorkCalendarDay.Create(date, dayType, null);

    private static Absence CreateAbsence(
        Guid employeeId,
        DateOnly start,
        DateOnly end,
        AbsenceType type)
        => Absence.Create(
            employeeId,
            new DateRange(start, end),
            type,
            null,
            null);

    private static Employee CreateEmployee(
        bool isActive = true,
        DateOnly? hireDate = null,
        DateOnly? terminationDate = null)
    {
        var employee = new Employee();
        var id = Guid.NewGuid();

        SetEmployeeProperty(employee, nameof(Employee.Id), id);
        SetEmployeeProperty(employee, nameof(Employee.EmployeeCode), $"EMP-{id:N}"[..12]);
        SetEmployeeProperty(employee, nameof(Employee.FirstName), "Ana");
        SetEmployeeProperty(employee, nameof(Employee.LastName), "Perez");
        SetEmployeeProperty(employee, nameof(Employee.IsActive), isActive);
        SetEmployeeProperty(
            employee,
            nameof(Employee.HireDate),
            hireDate ?? new DateOnly(2024, 1, 10));
        SetEmployeeProperty(employee, nameof(Employee.TerminationDate), terminationDate);

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
