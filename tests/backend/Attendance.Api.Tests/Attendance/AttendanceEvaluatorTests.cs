using System.Reflection;
using Attendance.Api.Modules.Absences.Domain;
using Attendance.Api.Modules.Attendance.Domain;
using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.WorkCalendar.Domain;
using Xunit;

namespace Attendance.Api.Tests.Attendance;

public sealed class AttendanceEvaluatorTests
{
    private static readonly AttendanceEvaluator Evaluator = new();

    public static TheoryData<AbsenceType, AttendanceStatus> AuthorizedAbsenceStatuses => new()
    {
        { AbsenceType.Vacation, AttendanceStatus.Vacation },
        { AbsenceType.MedicalLeave, AttendanceStatus.MedicalLeave },
        { AbsenceType.Permission, AttendanceStatus.Permission },
        { AbsenceType.Commission, AttendanceStatus.Commission },
        { AbsenceType.JustifiedAbsence, AttendanceStatus.JustifiedAbsence }
    };

    public static TheoryData<AttendanceMarkType[], AttendanceStatus, AttendanceAnomaly[]> HolidayScenarios => new()
    {
        { [], AttendanceStatus.Holiday, [] },
        { [AttendanceMarkType.Entry], AttendanceStatus.Holiday, [AttendanceAnomaly.MarksOnHoliday] },
        { [AttendanceMarkType.Entry, AttendanceMarkType.Exit], AttendanceStatus.Holiday, [AttendanceAnomaly.MarksOnHoliday] }
    };

    public static TheoryData<AttendanceMarkType[], AttendanceStatus, AttendanceAnomaly[]> NonWorkingDayScenarios => new()
    {
        { [], AttendanceStatus.NonWorkingDay, [] },
        { [AttendanceMarkType.Entry], AttendanceStatus.NonWorkingDay, [AttendanceAnomaly.MarksOnNonWorkingDay] }
    };

    public static TheoryData<AttendanceMarkType[], AttendanceStatus, AttendanceAnomaly[]> WorkingDayScenarios => new()
    {
        { [AttendanceMarkType.Entry, AttendanceMarkType.Exit], AttendanceStatus.Present, [] },
        { [AttendanceMarkType.Entry], AttendanceStatus.Incomplete, [AttendanceAnomaly.IncompleteMarks] },
        { [AttendanceMarkType.Exit], AttendanceStatus.Incomplete, [AttendanceAnomaly.IncompleteMarks] },
        { [], AttendanceStatus.UnexcusedAbsence, [] },
        { [AttendanceMarkType.Entry, AttendanceMarkType.LunchStart, AttendanceMarkType.LunchEnd, AttendanceMarkType.Exit], AttendanceStatus.Present, [] },
        { [AttendanceMarkType.Entry, AttendanceMarkType.CommissionExit, AttendanceMarkType.CommissionReturn, AttendanceMarkType.Exit], AttendanceStatus.Present, [] },
        { [AttendanceMarkType.Entry, AttendanceMarkType.OtherExit, AttendanceMarkType.OtherReturn, AttendanceMarkType.Exit], AttendanceStatus.Present, [] },
        { [AttendanceMarkType.LunchStart, AttendanceMarkType.LunchEnd], AttendanceStatus.Incomplete, [AttendanceAnomaly.IncompleteMarks] },
        { [AttendanceMarkType.CommissionReturn], AttendanceStatus.Incomplete, [AttendanceAnomaly.IncompleteMarks] }
    };

    [Fact]
    public void Evaluate_DateBeforeHireDate_ReturnsNotApplicable()
    {
        var employee = CreateEmployee(
            hireDate: new DateOnly(2026, 8, 10),
            terminationDate: null);

        var result = Evaluate(
            employee,
            new DateOnly(2026, 8, 9),
            workCalendarDay: null);

        AssertSuccess(
            result,
            AttendanceStatus.NotApplicable);
    }

    [Fact]
    public void Evaluate_DateAfterTerminationDate_ReturnsNotApplicable()
    {
        var employee = CreateEmployee(
            hireDate: new DateOnly(2026, 1, 10),
            terminationDate: new DateOnly(2026, 8, 10));

        var result = Evaluate(
            employee,
            new DateOnly(2026, 8, 11),
            workCalendarDay: null);

        AssertSuccess(
            result,
            AttendanceStatus.NotApplicable);
    }

    [Fact]
    public void Evaluate_HireDateExactDate_IsEvaluable()
    {
        var date = new DateOnly(2026, 8, 10);
        var employee = CreateEmployee(
            hireDate: date,
            terminationDate: null);

        var result = Evaluate(
            employee,
            date,
            CreateWorkCalendarDay(date, DayType.WorkingDay));

        AssertSuccess(
            result,
            AttendanceStatus.UnexcusedAbsence);
    }

    [Fact]
    public void Evaluate_TerminationDateExactDate_IsEvaluable()
    {
        var date = new DateOnly(2026, 8, 10);
        var employee = CreateEmployee(
            hireDate: new DateOnly(2026, 1, 10),
            terminationDate: date);

        var result = Evaluate(
            employee,
            date,
            CreateWorkCalendarDay(date, DayType.WorkingDay));

        AssertSuccess(
            result,
            AttendanceStatus.UnexcusedAbsence);
    }

    [Fact]
    public void Evaluate_MissingWorkCalendarDay_ReturnsFailure()
    {
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        var result = Evaluate(
            employee,
            date,
            workCalendarDay: null);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Status);
        Assert.Empty(result.Anomalies);
        Assert.Equal(AttendanceEvaluationFailure.MissingWorkCalendarDay, result.Failure);
    }

    [Theory]
    [MemberData(nameof(HolidayScenarios))]
    public void Evaluate_HolidayScenarios_ReturnExpectedResult(
        AttendanceMarkType[] markTypes,
        AttendanceStatus expectedStatus,
        AttendanceAnomaly[] expectedAnomalies)
    {
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        var result = Evaluate(
            employee,
            date,
            CreateWorkCalendarDay(date, DayType.Holiday),
            marks: CreateMarks(employee.Id, date, markTypes));

        AssertSuccess(result, expectedStatus, expectedAnomalies);
    }

    [Theory]
    [MemberData(nameof(NonWorkingDayScenarios))]
    public void Evaluate_NonWorkingDayScenarios_ReturnExpectedResult(
        AttendanceMarkType[] markTypes,
        AttendanceStatus expectedStatus,
        AttendanceAnomaly[] expectedAnomalies)
    {
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        var result = Evaluate(
            employee,
            date,
            CreateWorkCalendarDay(date, DayType.NonWorkingDay),
            marks: CreateMarks(employee.Id, date, markTypes));

        AssertSuccess(result, expectedStatus, expectedAnomalies);
    }

    [Theory]
    [MemberData(nameof(AuthorizedAbsenceStatuses))]
    public void Evaluate_AuthorizedAbsenceWithoutMarks_ReturnsMappedStatus(
        AbsenceType absenceType,
        AttendanceStatus expectedStatus)
    {
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        var result = Evaluate(
            employee,
            date,
            CreateWorkCalendarDay(date, DayType.WorkingDay),
            effectiveAbsence: CreateAbsence(employee.Id, date, absenceType));

        AssertSuccess(result, expectedStatus);
    }

    [Theory]
    [InlineData(AbsenceType.Vacation)]
    [InlineData(AbsenceType.MedicalLeave)]
    public void Evaluate_AuthorizedAbsenceWithMarks_PreservesAbsenceAndAddsAnomaly(
        AbsenceType absenceType)
    {
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        var result = Evaluate(
            employee,
            date,
            CreateWorkCalendarDay(date, DayType.WorkingDay),
            CreateAbsence(employee.Id, date, absenceType),
            CreateMarks(
                employee.Id,
                date,
                AttendanceMarkType.Entry,
                AttendanceMarkType.Exit));

        AssertSuccess(
            result,
            absenceType switch
            {
                AbsenceType.Vacation => AttendanceStatus.Vacation,
                AbsenceType.MedicalLeave => AttendanceStatus.MedicalLeave,
                _ => throw new ArgumentOutOfRangeException(nameof(absenceType))
            },
            AttendanceAnomaly.MarksDuringAuthorizedAbsence);
    }

    [Theory]
    [MemberData(nameof(WorkingDayScenarios))]
    public void Evaluate_WorkingDayScenarios_ReturnExpectedResult(
        AttendanceMarkType[] markTypes,
        AttendanceStatus expectedStatus,
        AttendanceAnomaly[] expectedAnomalies)
    {
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        var result = Evaluate(
            employee,
            date,
            CreateWorkCalendarDay(date, DayType.WorkingDay),
            marks: CreateMarks(employee.Id, date, markTypes));

        AssertSuccess(result, expectedStatus, expectedAnomalies);
    }

    [Fact]
    public void Evaluate_AuthorizedAbsenceTakesPrecedenceOverHoliday()
    {
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        var result = Evaluate(
            employee,
            date,
            CreateWorkCalendarDay(date, DayType.Holiday),
            CreateAbsence(employee.Id, date, AbsenceType.Vacation));

        AssertSuccess(
            result,
            AttendanceStatus.Vacation);
    }

    private static DailyAttendanceResult Evaluate(
        Employee employee,
        DateOnly date,
        WorkCalendarDay? workCalendarDay,
        Absence? effectiveAbsence = null,
        IReadOnlyCollection<AttendanceMark>? marks = null)
        => Evaluator.Evaluate(
            new AttendanceEvaluationContext(
                employee,
                date,
                workCalendarDay,
                effectiveAbsence,
                marks));

    private static Employee CreateEmployee(
        DateOnly? hireDate = null,
        DateOnly? terminationDate = null)
    {
        var employee = new Employee();
        var id = Guid.NewGuid();

        SetEmployeeProperty(employee, nameof(Employee.Id), id);
        SetEmployeeProperty(employee, nameof(Employee.EmployeeCode), $"EMP-{id:N}"[..12]);
        SetEmployeeProperty(employee, nameof(Employee.FirstName), "Ana");
        SetEmployeeProperty(employee, nameof(Employee.LastName), "Perez");
        SetEmployeeProperty(employee, nameof(Employee.IsActive), true);
        SetEmployeeProperty(
            employee,
            nameof(Employee.HireDate),
            hireDate ?? new DateOnly(2024, 1, 10));
        SetEmployeeProperty(employee, nameof(Employee.TerminationDate), terminationDate);

        return employee;
    }

    private static WorkCalendarDay CreateWorkCalendarDay(
        DateOnly date,
        DayType dayType)
        => WorkCalendarDay.Create(date, dayType, null);

    private static Absence CreateAbsence(
        Guid employeeId,
        DateOnly date,
        AbsenceType absenceType)
        => Absence.Create(
            employeeId,
            new DateRange(date, date),
            absenceType,
            null,
            null);

    private static IReadOnlyCollection<AttendanceMark> CreateMarks(
        Guid employeeId,
        DateOnly date,
        params AttendanceMarkType[] markTypes)
        => markTypes
            .Select((markType, index) =>
                CreateMark(
                    employeeId,
                    new DateTimeOffset(
                        date.ToDateTime(TimeOnly.MinValue).AddMinutes(index + 1),
                        TimeSpan.Zero),
                    markType))
            .ToArray();

    private static AttendanceMark CreateMark(
        Guid employeeId,
        DateTimeOffset occurredAt,
        AttendanceMarkType type)
    {
        var mark = new AttendanceMark();

        SetMarkProperty(mark, nameof(AttendanceMark.Id), Guid.NewGuid());
        SetMarkProperty(mark, nameof(AttendanceMark.EmployeeId), employeeId);
        SetMarkProperty(mark, nameof(AttendanceMark.OccurredAt), occurredAt);
        SetMarkProperty(mark, nameof(AttendanceMark.Type), type);
        SetMarkProperty(mark, nameof(AttendanceMark.Source), AttendanceSource.Manual);
        SetMarkProperty<Guid?>(mark, nameof(AttendanceMark.CheckpointId), null);

        return mark;
    }

    private static void AssertSuccess(
        DailyAttendanceResult result,
        AttendanceStatus expectedStatus,
        params AttendanceAnomaly[] expectedAnomalies)
    {
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedStatus, result.Status);
        Assert.Null(result.Failure);
        Assert.Equal(expectedAnomalies, result.Anomalies);
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

    private static void SetMarkProperty<T>(
        AttendanceMark mark,
        string propertyName,
        T value)
        => typeof(AttendanceMark)
            .GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(mark, value);
}
