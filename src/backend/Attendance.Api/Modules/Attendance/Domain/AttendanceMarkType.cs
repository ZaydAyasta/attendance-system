namespace Attendance.Api.Modules.Attendance.Domain;

/// <summary>
/// Defines the business meaning of an attendance mark.
/// </summary>
public enum AttendanceMarkType
{
    Entry = 1,
    LunchStart = 2,
    LunchEnd = 3,
    Exit = 4,

    CommissionExit = 5,
    CommissionReturn = 6,

    OtherExit = 7,
    OtherReturn = 8
}