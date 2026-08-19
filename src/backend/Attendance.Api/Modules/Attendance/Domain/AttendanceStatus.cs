namespace Attendance.Api.Modules.Attendance.Domain;

/// <summary>
/// Defines the evaluated attendance status of an employee for a calendar date.
/// </summary>
public enum AttendanceStatus
{
    Present = 1,
    UnexcusedAbsence = 2,

    Vacation = 3,
    MedicalLeave = 4,
    Permission = 5,
    Commission = 6,

    Holiday = 7,
    NonWorkingDay = 8
}