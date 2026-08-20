namespace Attendance.Api.Modules.Attendance.Domain;

/// <summary>
/// Identifies anomalies detected during daily attendance evaluation.
/// </summary>
public enum AttendanceAnomaly
{
    None = 0,
    IncompleteMarks = 1,
    MarksOnHoliday = 2,
    MarksOnNonWorkingDay = 3,
    MarksDuringAuthorizedAbsence = 4
}
