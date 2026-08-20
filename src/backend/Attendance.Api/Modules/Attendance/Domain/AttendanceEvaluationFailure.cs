namespace Attendance.Api.Modules.Attendance.Domain;

/// <summary>
/// Identifies why a daily attendance evaluation could not be completed.
/// </summary>
public enum AttendanceEvaluationFailure
{
    MissingWorkCalendarDay = 1
}
