using System.Reflection;
using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Absences.Domain;
using Attendance.Api.Modules.Attendance.Application;
using Attendance.Api.Modules.Attendance.Contracts;
using Attendance.Api.Modules.Attendance.Domain;
using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.WorkCalendar.Domain;
using Attendance.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Attendance.Api.Tests.Attendance;

public sealed class DailyAttendanceServicePostgreSqlTests
    : IClassFixture<PostgreSqlAttendanceDatabaseFixture>
{
    private static readonly AttendanceTimeZone AttendanceTimeZone =
        new("America/Lima");

    private readonly PostgreSqlAttendanceDatabaseFixture fixture;

    public DailyAttendanceServicePostgreSqlTests(
        PostgreSqlAttendanceDatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_ActiveAbsenceIsRecoveredByDateRange()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var absence = CreateAbsence(
            employee.Id,
            new DateOnly(2026, 8, 20),
            new DateOnly(2026, 8, 22),
            AbsenceType.Vacation);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(
            CreateWorkCalendarDay(new DateOnly(2026, 8, 21), DayType.WorkingDay));
        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(new DateOnly(2026, 8, 21)),
            CancellationToken.None);

        AssertSuccess(result, AttendanceStatus.Vacation);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_CancelledAbsenceIsIgnored()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);
        var absence = CreateAbsence(employee.Id, date, date, AbsenceType.Vacation);
        absence.Cancel();

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(result, AttendanceStatus.UnexcusedAbsence);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_AllowsHistoricalQueryForInactiveEmployee()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee(
            isActive: false,
            terminationDate: new DateOnly(2026, 8, 25));
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(result, AttendanceStatus.UnexcusedAbsence);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_MissingCalendar_ReturnsFailure()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertFailure(result, AttendanceEvaluationFailure.MissingWorkCalendarDay, date);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_DateBeforeHireDate_ReturnsNotApplicable()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee(hireDate: new DateOnly(2026, 8, 21));
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(result, AttendanceStatus.NotApplicable);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_DateAfterTerminationDate_ReturnsNotApplicable()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee(terminationDate: new DateOnly(2026, 8, 19));
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(result, AttendanceStatus.NotApplicable);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_WorkingDayWithoutMarks_ReturnsUnexcusedAbsence()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(result, AttendanceStatus.UnexcusedAbsence);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_HolidayWithoutMarks_ReturnsHoliday()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.Holiday));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(result, AttendanceStatus.Holiday);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_HolidayWithMarks_ReturnsMarksOnHoliday()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.Holiday));
        dbContext.AttendanceMarks.Add(
            CreateMark(employee.Id, new DateTimeOffset(2026, 8, 20, 13, 0, 0, TimeSpan.Zero), AttendanceMarkType.Entry));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(
            result,
            AttendanceStatus.Holiday,
            AttendanceAnomaly.MarksOnHoliday);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_NonWorkingDayWithoutMarks_ReturnsNonWorkingDay()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.NonWorkingDay));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(result, AttendanceStatus.NonWorkingDay);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_ActiveVacationWithMarks_ReturnsAnomaly()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        dbContext.Absences.Add(CreateAbsence(employee.Id, date, date, AbsenceType.Vacation));
        dbContext.AttendanceMarks.AddRange(
            CreateMark(employee.Id, new DateTimeOffset(2026, 8, 20, 13, 0, 0, TimeSpan.Zero), AttendanceMarkType.Entry),
            CreateMark(employee.Id, new DateTimeOffset(2026, 8, 20, 22, 0, 0, TimeSpan.Zero), AttendanceMarkType.Exit));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(
            result,
            AttendanceStatus.Vacation,
            AttendanceAnomaly.MarksDuringAuthorizedAbsence);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_ActiveCommission_ReturnsCommission()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        dbContext.Absences.Add(CreateAbsence(employee.Id, date, date, AbsenceType.Commission));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(result, AttendanceStatus.Commission);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_ActiveJustifiedAbsence_ReturnsJustifiedAbsence()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        dbContext.Absences.Add(CreateAbsence(employee.Id, date, date, AbsenceType.JustifiedAbsence));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(result, AttendanceStatus.JustifiedAbsence);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_MarksInsideLocalDay_AreIncluded()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        dbContext.AttendanceMarks.AddRange(
            CreateMark(employee.Id, new DateTimeOffset(2026, 8, 20, 5, 1, 0, TimeSpan.Zero), AttendanceMarkType.Entry),
            CreateMark(employee.Id, new DateTimeOffset(2026, 8, 20, 22, 0, 0, TimeSpan.Zero), AttendanceMarkType.Exit));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(result, AttendanceStatus.Present);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_MarksOutsideRequestedLocalDate_AreExcluded()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        dbContext.AttendanceMarks.Add(
            CreateMark(employee.Id, new DateTimeOffset(2026, 8, 20, 2, 0, 0, TimeSpan.Zero), AttendanceMarkType.Entry));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(result, AttendanceStatus.UnexcusedAbsence);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_MarkAtStartInclusive_IsIncluded()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        dbContext.AttendanceMarks.AddRange(
            CreateMark(employee.Id, new DateTimeOffset(2026, 8, 20, 5, 0, 0, TimeSpan.Zero), AttendanceMarkType.Entry),
            CreateMark(employee.Id, new DateTimeOffset(2026, 8, 20, 15, 0, 0, TimeSpan.Zero), AttendanceMarkType.Exit));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(result, AttendanceStatus.Present);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_MarkAtEndExclusive_IsExcluded()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        dbContext.AttendanceMarks.AddRange(
            CreateMark(employee.Id, new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero), AttendanceMarkType.Entry),
            CreateMark(employee.Id, new DateTimeOffset(2026, 8, 21, 5, 0, 0, TimeSpan.Zero), AttendanceMarkType.Exit));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(
            result,
            AttendanceStatus.Incomplete,
            AttendanceAnomaly.IncompleteMarks);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetRangeAsync_LoadsCalendarAbsenceAndMarksAcrossRange()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var from = new DateOnly(2026, 8, 20);
        var to = new DateOnly(2026, 8, 22);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.AddRange(
            CreateWorkCalendarDay(from, DayType.WorkingDay),
            CreateWorkCalendarDay(from.AddDays(1), DayType.WorkingDay),
            CreateWorkCalendarDay(to, DayType.Holiday));
        dbContext.Absences.Add(CreateAbsence(
            employee.Id,
            from.AddDays(1),
            from.AddDays(1),
            AbsenceType.Permission));
        dbContext.AttendanceMarks.AddRange(
            CreateMark(employee.Id, new DateTimeOffset(2026, 8, 20, 13, 0, 0, TimeSpan.Zero), AttendanceMarkType.Entry),
            CreateMark(employee.Id, new DateTimeOffset(2026, 8, 20, 22, 0, 0, TimeSpan.Zero), AttendanceMarkType.Exit));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetRangeAsync(
            employee.Id,
            new AttendanceRangeQuery(from, to),
            CancellationToken.None);

        Assert.Equal(AttendanceQueryStatus.Success, result.Status);
        Assert.Equal(
            new[] { "Present", "Permission", "Holiday" },
            result.Value!.Days.Select(x => x.Status));
    }

    [RequiresContainerRuntimeFact]
    public async Task GetRangeAsync_ReturnsEveryDateInOrder()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var from = new DateOnly(2026, 8, 20);
        var to = new DateOnly(2026, 8, 22);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.AddRange(
            CreateWorkCalendarDay(from, DayType.WorkingDay),
            CreateWorkCalendarDay(from.AddDays(1), DayType.WorkingDay),
            CreateWorkCalendarDay(to, DayType.WorkingDay));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetRangeAsync(
            employee.Id,
            new AttendanceRangeQuery(from, to),
            CancellationToken.None);

        Assert.Equal(AttendanceQueryStatus.Success, result.Status);
        Assert.Equal(new[] { from, from.AddDays(1), to }, result.Value!.Days.Select(x => x.Date));
    }

    [RequiresContainerRuntimeFact]
    public async Task GetRangeAsync_AbsenceAffectsOnlyIntersectingDates()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var from = new DateOnly(2026, 8, 20);
        var to = new DateOnly(2026, 8, 23);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.AddRange(
            CreateWorkCalendarDay(from, DayType.WorkingDay),
            CreateWorkCalendarDay(from.AddDays(1), DayType.WorkingDay),
            CreateWorkCalendarDay(from.AddDays(2), DayType.WorkingDay),
            CreateWorkCalendarDay(to, DayType.WorkingDay));
        dbContext.Absences.Add(CreateAbsence(
            employee.Id,
            from.AddDays(1),
            from.AddDays(2),
            AbsenceType.Vacation));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetRangeAsync(
            employee.Id,
            new AttendanceRangeQuery(from, to),
            CancellationToken.None);

        Assert.Equal(
            new[] { "UnexcusedAbsence", "Vacation", "Vacation", "UnexcusedAbsence" },
            result.Value!.Days.Select(x => x.Status));
    }

    [RequiresContainerRuntimeFact]
    public async Task GetRangeAsync_CancelledAbsence_IsIgnored()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var from = new DateOnly(2026, 8, 20);
        var absence = CreateAbsence(
            employee.Id,
            from.AddDays(1),
            from.AddDays(1),
            AbsenceType.Vacation);
        absence.Cancel();

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.AddRange(
            CreateWorkCalendarDay(from, DayType.WorkingDay),
            CreateWorkCalendarDay(from.AddDays(1), DayType.WorkingDay));
        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetRangeAsync(
            employee.Id,
            new AttendanceRangeQuery(from, from.AddDays(1)),
            CancellationToken.None);

        Assert.Equal(
            new[] { "UnexcusedAbsence", "UnexcusedAbsence" },
            result.Value!.Days.Select(x => x.Status));
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_MultipleActiveAbsences_ReturnsFailure()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        dbContext.Absences.AddRange(
            CreateAbsence(employee.Id, date, date, AbsenceType.Vacation),
            CreateAbsence(employee.Id, date, date, AbsenceType.Permission));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertFailure(result, AttendanceEvaluationFailure.MultipleActiveAbsences, date);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_UsesRealXminMapping()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);
        var absence = CreateAbsence(employee.Id, date, date, AbsenceType.MedicalLeave);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        dbContext.Absences.Add(absence);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var persistedAbsence = await dbContext.Absences
            .AsNoTracking()
            .SingleAsync(x => x.Id == absence.Id);

        Assert.True(persistedAbsence.Version > 0);

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(result, AttendanceStatus.MedicalLeave);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_DateAfterTerminationDateWithMultipleActiveAbsences_ReturnsNotApplicable()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee(terminationDate: new DateOnly(2026, 8, 19));
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        dbContext.Absences.AddRange(
            CreateAbsence(employee.Id, date, date, AbsenceType.Vacation),
            CreateAbsence(employee.Id, date, date, AbsenceType.Permission));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(result, AttendanceStatus.NotApplicable);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_DateBeforeHireDateWithMultipleActiveAbsences_ReturnsNotApplicable()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee(hireDate: new DateOnly(2026, 8, 21));
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        dbContext.Absences.AddRange(
            CreateAbsence(employee.Id, date, date, AbsenceType.Vacation),
            CreateAbsence(employee.Id, date, date, AbsenceType.Permission));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertSuccess(result, AttendanceStatus.NotApplicable);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_MissingCalendarWithMultipleActiveAbsences_ReturnsMissingCalendar()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.Absences.AddRange(
            CreateAbsence(employee.Id, date, date, AbsenceType.Vacation),
            CreateAbsence(employee.Id, date, date, AbsenceType.Permission));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertFailure(result, AttendanceEvaluationFailure.MissingWorkCalendarDay, date);
    }

    [RequiresContainerRuntimeFact]
    public async Task GetByDateAsync_ApplicableDateWithCalendarAndMultipleActiveAbsences_ReturnsMultipleActiveAbsences()
    {
        await using var dbContext = await CreateDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 20);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(CreateWorkCalendarDay(date, DayType.WorkingDay));
        dbContext.Absences.AddRange(
            CreateAbsence(employee.Id, date, date, AbsenceType.Vacation),
            CreateAbsence(employee.Id, date, date, AbsenceType.Permission));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        AssertFailure(result, AttendanceEvaluationFailure.MultipleActiveAbsences, date);
    }

    private async Task<AttendanceDbContext> CreateDbContextAsync()
    {
        fixture.ThrowIfUnavailable();
        await fixture.ResetAsync();

        return fixture.CreateDbContext();
    }

    private static DailyAttendanceService CreateService(
        AttendanceDbContext dbContext)
        => new(
            dbContext,
            new AttendanceEvaluator(),
            new AttendanceTimeCalculator(),
            AttendanceTimeZone);

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

    private static AttendanceMark CreateMark(
        Guid employeeId,
        DateTimeOffset occurredAt,
        AttendanceMarkType type)
        => AttendanceMark.Create(
            employeeId,
            occurredAt,
            type,
            AttendanceSource.Manual,
            checkpointId: null);

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

    private static void AssertSuccess(
        AttendanceQueryResult<DailyAttendanceResponse> result,
        AttendanceStatus expectedStatus,
        params AttendanceAnomaly[] expectedAnomalies)
    {
        Assert.Equal(AttendanceQueryStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedStatus.ToString(), result.Value!.Status);
        Assert.Null(result.Value.Failure);
        Assert.Equal(
            expectedAnomalies.Select(x => x.ToString()),
            result.Value.Anomalies);
    }

    private static void AssertFailure(
        AttendanceQueryResult<DailyAttendanceResponse> result,
        AttendanceEvaluationFailure expectedFailure,
        DateOnly expectedDate)
    {
        Assert.Equal(AttendanceQueryStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedDate, result.Value!.Date);
        Assert.Null(result.Value.Status);
        Assert.Empty(result.Value.Anomalies);
        Assert.Equal(expectedFailure.ToString(), result.Value.Failure);
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
