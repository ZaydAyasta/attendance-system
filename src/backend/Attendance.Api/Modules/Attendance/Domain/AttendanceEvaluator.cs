using Attendance.Api.Modules.Absences.Domain;
using Attendance.Api.Modules.WorkCalendar.Domain;

namespace Attendance.Api.Modules.Attendance.Domain;

/// <summary>
/// Evaluates the daily attendance status for an employee using already-loaded domain data.
/// </summary>
public sealed class AttendanceEvaluator
{
    public DailyAttendanceResult Evaluate(AttendanceEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (IsOutsideEmployment(context))
        {
            return DailyAttendanceResult.Success(
                context.Employee.Id,
                context.Date,
                AttendanceStatus.NotApplicable);
        }

        if (!context.EffectiveDayType.HasValue)
        {
            return DailyAttendanceResult.FailureResult(
                context.Employee.Id,
                context.Date,
                AttendanceEvaluationFailure.MissingWorkCalendarDay);
        }

        if (context.EffectiveAbsence is not null)
        {
            return DailyAttendanceResult.Success(
                context.Employee.Id,
                context.Date,
                MapAbsenceStatus(context.EffectiveAbsence.Type),
                context.Marks.Count > 0
                    ? [AttendanceAnomaly.MarksDuringAuthorizedAbsence]
                    : []);
        }

        return context.EffectiveDayType.Value switch
        {
            DayType.Holiday => DailyAttendanceResult.Success(
                context.Employee.Id,
                context.Date,
                AttendanceStatus.Holiday,
                context.Marks.Count > 0
                    ? [AttendanceAnomaly.MarksOnHoliday]
                    : []),
            DayType.NonWorkingDay => DailyAttendanceResult.Success(
                context.Employee.Id,
                context.Date,
                AttendanceStatus.NonWorkingDay,
                context.Marks.Count > 0
                    ? [AttendanceAnomaly.MarksOnNonWorkingDay]
                    : []),
            DayType.WorkingDay => EvaluateWorkingDay(context),
            _ => throw new ArgumentOutOfRangeException(
                nameof(context.EffectiveDayType),
                context.EffectiveDayType,
                "Unsupported work calendar day type.")
        };
    }

    private static DailyAttendanceResult EvaluateWorkingDay(
        AttendanceEvaluationContext context)
    {
        var hasEntry = context.Marks.Any(mark => mark.Type == AttendanceMarkType.Entry);
        var hasExit = context.Marks.Any(mark => mark.Type == AttendanceMarkType.Exit);

        if (hasEntry && hasExit)
        {
            return DailyAttendanceResult.Success(
                context.Employee.Id,
                context.Date,
                AttendanceStatus.Present);
        }

        if (context.Marks.Count > 0)
        {
            return DailyAttendanceResult.Success(
                context.Employee.Id,
                context.Date,
                AttendanceStatus.Incomplete,
                AttendanceAnomaly.IncompleteMarks);
        }

        return DailyAttendanceResult.Success(
            context.Employee.Id,
            context.Date,
            AttendanceStatus.UnexcusedAbsence);
    }

    private static bool IsOutsideEmployment(AttendanceEvaluationContext context)
        => context.Date < context.Employee.HireDate
           || (context.Employee.TerminationDate is not null
               && context.Date > context.Employee.TerminationDate.Value);

    private static AttendanceStatus MapAbsenceStatus(AbsenceType absenceType)
        => absenceType switch
        {
            AbsenceType.Vacation => AttendanceStatus.Vacation,
            AbsenceType.MedicalLeave => AttendanceStatus.MedicalLeave,
            AbsenceType.Permission => AttendanceStatus.Permission,
            AbsenceType.Commission => AttendanceStatus.Commission,
            AbsenceType.JustifiedAbsence => AttendanceStatus.JustifiedAbsence,
            _ => throw new ArgumentOutOfRangeException(
                nameof(absenceType),
                absenceType,
                "Unsupported absence type.")
        };
}
