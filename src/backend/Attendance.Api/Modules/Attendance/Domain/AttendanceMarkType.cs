namespace Attendance.Api.Modules.Attendance.Domain;

/// <summary>
/// Defines the business meaning of an attendance mark.
/// </summary>
public enum AttendanceMarkType
{
    /// <summary>
    /// Starts workplace presence for the day.
    /// </summary>
    Entry = 1,

    /// <summary>
    /// Starts a lunch interval that is excluded from worked time.
    /// </summary>
    LunchStart = 2,

    /// <summary>
    /// Ends a lunch interval that is excluded from worked time.
    /// </summary>
    LunchEnd = 3,

    /// <summary>
    /// Ends workplace presence for the day.
    /// </summary>
    Exit = 4,

    /// <summary>
    /// Starts a work-related commission interval that is still considered work time.
    /// </summary>
    CommissionExit = 5,

    /// <summary>
    /// Ends a work-related commission interval that is still considered work time.
    /// </summary>
    CommissionReturn = 6,

    /// <summary>
    /// Starts a temporary exit for other reasons; worked-time policy is defined elsewhere.
    /// </summary>
    OtherExit = 7,

    /// <summary>
    /// Ends a temporary exit for other reasons; worked-time policy is defined elsewhere.
    /// </summary>
    OtherReturn = 8
}
