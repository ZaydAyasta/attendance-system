using Attendance.Api.Modules.Absences.Domain;
using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.WorkCalendar.Domain;

namespace Attendance.Api.Modules.Attendance.Domain;

/// <summary>
/// Supplies the already-loaded domain data required to evaluate one employee day.
/// </summary>
public sealed class AttendanceEvaluationContext
{
    public AttendanceEvaluationContext(
        Employee employee,
        DateOnly date,
        WorkCalendarDay? workCalendarDay,
        DayType? effectiveDayTypeOverride,
        Absence? effectiveAbsence,
        IReadOnlyCollection<AttendanceMark>? marks)
    {
        Employee = employee ?? throw new ArgumentNullException(nameof(employee));

        if (date == default)
        {
            throw new ArgumentException(
                "Date must be a non-default value.",
                nameof(date));
        }

        if (workCalendarDay is not null && workCalendarDay.Date != date)
        {
            throw new ArgumentException(
                "WorkCalendarDay must match the evaluation date.",
                nameof(workCalendarDay));
        }

        if (effectiveDayTypeOverride.HasValue
            && !Enum.IsDefined(effectiveDayTypeOverride.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(effectiveDayTypeOverride),
                effectiveDayTypeOverride,
                "Unsupported effective day type.");
        }

        if (effectiveAbsence is not null)
        {
            if (effectiveAbsence.EmployeeId != employee.Id)
            {
                throw new ArgumentException(
                    "Effective absence must belong to the evaluated employee.",
                    nameof(effectiveAbsence));
            }

            if (!effectiveAbsence.Period.Contains(date))
            {
                throw new ArgumentException(
                    "Effective absence must contain the evaluation date.",
                    nameof(effectiveAbsence));
            }
        }

        var normalizedMarks = (marks ?? Array.Empty<AttendanceMark>()).ToArray();

        for (var index = 0; index < normalizedMarks.Length; index++)
        {
            if (normalizedMarks[index].EmployeeId != employee.Id)
            {
                throw new ArgumentException(
                    "All marks must belong to the evaluated employee.",
                    nameof(marks));
            }
        }

        Date = date;
        WorkCalendarDay = workCalendarDay;
        EffectiveDayType = effectiveDayTypeOverride ?? workCalendarDay?.DayType;
        EffectiveAbsence = effectiveAbsence;
        Marks = normalizedMarks;
    }

    public Employee Employee { get; }

    public DateOnly Date { get; }

    public WorkCalendarDay? WorkCalendarDay { get; }

    public DayType? EffectiveDayType { get; }

    /// <summary>
    /// Gets the already-selected effective absence for the date, if any.
    /// The evaluator intentionally does not interpret AbsenceStatus workflows.
    /// </summary>
    public Absence? EffectiveAbsence { get; }

    public IReadOnlyCollection<AttendanceMark> Marks { get; }
}
