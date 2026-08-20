namespace Attendance.Api.Modules.Attendance.Domain;

/// <summary>
/// Defines the evaluated attendance status of an employee for a calendar date.
/// </summary>
public enum AttendanceStatus
{
    Present = 1,
    Incomplete = 2,
    UnexcusedAbsence = 3,
    Vacation = 4,
    MedicalLeave = 5,
    Permission = 6,
    Commission = 7,
    JustifiedAbsence = 8,
    Holiday = 9,
    NonWorkingDay = 10,
    NotApplicable = 11
}
