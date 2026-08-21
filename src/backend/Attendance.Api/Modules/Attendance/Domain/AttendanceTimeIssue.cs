namespace Attendance.Api.Modules.Attendance.Domain;

/// <summary>
/// Represents an objective issue that prevents a complete worked-time calculation.
/// </summary>
public enum AttendanceTimeIssue
{
    NoAttendanceMarks = 1,
    MissingEntry = 2,
    MissingExit = 3,
    MultipleEntries = 4,
    MultipleExits = 5,
    ExitBeforeEntry = 6,
    MissingLunchEnd = 7,
    LunchEndWithoutLunchStart = 8,
    OverlappingLunch = 9,
    UnresolvedOtherExit = 10,
    OtherReturnWithoutOtherExit = 11
}
