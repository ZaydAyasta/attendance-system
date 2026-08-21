using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Absences.Domain;
using Attendance.Api.Modules.Attendance.Contracts;
using Attendance.Api.Modules.Attendance.Domain;
using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.WorkCalendar.Domain;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Api.Modules.Attendance.Application;

public sealed class DailyAttendanceService(
    AttendanceDbContext dbContext,
    AttendanceEvaluator evaluator,
    AttendanceTimeCalculator timeCalculator,
    AttendanceTimeZone attendanceTimeZone)
{
    public async Task<AttendanceQueryResult<DailyAttendanceResponse>> GetByDateAsync(
        Guid employeeId,
        DailyAttendanceQuery query,
        CancellationToken cancellationToken)
    {
        var employee = await GetEmployeeAsync(employeeId, cancellationToken);

        if (employee is null)
        {
            return new AttendanceQueryResult<DailyAttendanceResponse>(
                AttendanceQueryStatus.EmployeeNotFound);
        }

        var workCalendarDay = await dbContext.WorkCalendarDays
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Date == query.Date, cancellationToken);

        var absences = await LoadActiveAbsencesForDateAsync(
            employeeId,
            query.Date,
            cancellationToken);

        var dayStart = attendanceTimeZone.GetStartOfDay(query.Date);
        var dayEndExclusive = attendanceTimeZone.GetStartOfNextDay(query.Date);

        var marks = await LoadMarksAsync(
            employeeId,
            dayStart,
            dayEndExclusive,
            cancellationToken);

        var result = EvaluateDate(
            employee,
            query.Date,
            workCalendarDay,
            absences,
            marks);
        var timeResult = timeCalculator.Calculate(marks);

        return new AttendanceQueryResult<DailyAttendanceResponse>(
            AttendanceQueryStatus.Success,
            Map(result, timeResult));
    }

    public async Task<AttendanceQueryResult<EmployeeAttendanceRangeResponse>> GetRangeAsync(
        Guid employeeId,
        AttendanceRangeQuery query,
        CancellationToken cancellationToken)
    {
        var employee = await GetEmployeeAsync(employeeId, cancellationToken);

        if (employee is null)
        {
            return new AttendanceQueryResult<EmployeeAttendanceRangeResponse>(
                AttendanceQueryStatus.EmployeeNotFound);
        }

        var workCalendarDays = await dbContext.WorkCalendarDays
            .AsNoTracking()
            .Where(x => x.Date >= query.From && x.Date <= query.To)
            .ToDictionaryAsync(x => x.Date, cancellationToken);

        var activeAbsences = await LoadActiveAbsencesForRangeAsync(
            employeeId,
            query.From,
            query.To,
            cancellationToken);

        var rangeStart = attendanceTimeZone.GetStartOfDay(query.From);
        var rangeEndExclusive = attendanceTimeZone.GetStartOfNextDay(query.To);

        var marks = await LoadMarksAsync(
            employeeId,
            rangeStart,
            rangeEndExclusive,
            cancellationToken);

        var absencesByDate = ExpandAbsencesByDate(
            activeAbsences,
            query.From,
            query.To);
        var marksByDate = marks
            .GroupBy(mark => attendanceTimeZone.GetLocalDate(mark.OccurredAt))
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyCollection<AttendanceMark>)x.ToArray());

        var days = new List<DailyAttendanceResponse>();

        for (var date = query.From; date <= query.To; date = date.AddDays(1))
        {
            workCalendarDays.TryGetValue(date, out var workCalendarDay);
            absencesByDate.TryGetValue(date, out var absencesForDate);
            marksByDate.TryGetValue(date, out var marksForDate);

            var result = EvaluateDate(
                employee,
                date,
                workCalendarDay,
                absencesForDate ?? Array.Empty<Absence>(),
                marksForDate ?? Array.Empty<AttendanceMark>());
            var timeResult = timeCalculator.Calculate(
                marksForDate ?? Array.Empty<AttendanceMark>());

            days.Add(Map(result, timeResult));
        }

        return new AttendanceQueryResult<EmployeeAttendanceRangeResponse>(
            AttendanceQueryStatus.Success,
            new EmployeeAttendanceRangeResponse(
                employeeId,
                query.From,
                query.To,
                days));
    }

    private async Task<Employee?> GetEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
        => await dbContext.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == employeeId, cancellationToken);

    private async Task<List<Absence>> LoadActiveAbsencesForDateAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken)
        => (await dbContext.Absences
            .AsNoTracking()
            .Where(x =>
                x.EmployeeId == employeeId
                && x.Status == AbsenceStatus.Active)
            .ToListAsync(cancellationToken))
            .Where(x => x.Period.Contains(date))
            .OrderBy(x => x.Period.Start)
            .ThenBy(x => x.Period.End)
            .Take(2)
            .ToList();

    private async Task<List<Absence>> LoadActiveAbsencesForRangeAsync(
        Guid employeeId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
        => (await dbContext.Absences
            .AsNoTracking()
            .Where(x =>
                x.EmployeeId == employeeId
                && x.Status == AbsenceStatus.Active)
            .ToListAsync(cancellationToken))
            .Where(x => x.Period.Start <= to && x.Period.End >= from)
            .OrderBy(x => x.Period.Start)
            .ThenBy(x => x.Period.End)
            .ToList();

    private async Task<List<AttendanceMark>> LoadMarksAsync(
        Guid employeeId,
        DateTimeOffset startInclusive,
        DateTimeOffset endExclusive,
        CancellationToken cancellationToken)
        => (await dbContext.AttendanceMarks
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .ToListAsync(cancellationToken))
            .Where(x => x.OccurredAt >= startInclusive && x.OccurredAt < endExclusive)
            .OrderBy(x => x.OccurredAt)
            .ToList();

    private DailyAttendanceResult EvaluateDate(
        Employee employee,
        DateOnly date,
        WorkCalendarDay? workCalendarDay,
        IReadOnlyCollection<Absence> absences,
        IReadOnlyCollection<AttendanceMark> marks)
    {
        var preAbsenceResult = evaluator.Evaluate(
            new AttendanceEvaluationContext(
                employee,
                date,
                workCalendarDay,
                null,
                marks));

        if (preAbsenceResult.Status == AttendanceStatus.NotApplicable
            || preAbsenceResult.Failure == AttendanceEvaluationFailure.MissingWorkCalendarDay)
        {
            return preAbsenceResult;
        }

        if (absences.Count == 0)
        {
            return preAbsenceResult;
        }

        if (absences.Count > 1)
        {
            return DailyAttendanceResult.FailureResult(
                employee.Id,
                date,
                AttendanceEvaluationFailure.MultipleActiveAbsences);
        }

        return evaluator.Evaluate(
            new AttendanceEvaluationContext(
                employee,
                date,
                workCalendarDay,
                absences.SingleOrDefault(),
                marks));
    }

    private static Dictionary<DateOnly, IReadOnlyCollection<Absence>> ExpandAbsencesByDate(
        IReadOnlyCollection<Absence> absences,
        DateOnly from,
        DateOnly to)
    {
        var result = new Dictionary<DateOnly, List<Absence>>();

        foreach (var absence in absences)
        {
            var current = absence.Period.Start < from
                ? from
                : absence.Period.Start;
            var end = absence.Period.End > to
                ? to
                : absence.Period.End;

            while (current <= end)
            {
                if (!result.TryGetValue(current, out var absencesForDate))
                {
                    absencesForDate = new List<Absence>();
                    result[current] = absencesForDate;
                }

                absencesForDate.Add(absence);
                current = current.AddDays(1);
            }
        }

        return result.ToDictionary(
            x => x.Key,
            x => (IReadOnlyCollection<Absence>)x.Value);
    }

    private static DailyAttendanceResponse Map(
        DailyAttendanceResult result,
        DailyWorkedTimeResult timeResult)
    {
        var suppressNoAttendanceMarks =
            timeResult.Issues.Count == 1
            && timeResult.Issues.Contains(AttendanceTimeIssue.NoAttendanceMarks);

        return new(
            result.EmployeeId,
            result.Date,
            result.Status?.ToString(),
            result.Anomalies
                .Where(x => x != AttendanceAnomaly.None)
                .Select(x => x.ToString())
                .ToArray(),
            result.Failure?.ToString(),
            suppressNoAttendanceMarks
                ? null
                : timeResult.GrossMinutes,
            suppressNoAttendanceMarks
                ? null
                : timeResult.LunchMinutes,
            suppressNoAttendanceMarks
                ? null
                : timeResult.WorkedMinutes,
            suppressNoAttendanceMarks
                ? false
                : timeResult.IsComplete,
            suppressNoAttendanceMarks
                ? Array.Empty<string>()
                : timeResult.Issues.Select(x => x.ToString()).ToArray());
    }
}
