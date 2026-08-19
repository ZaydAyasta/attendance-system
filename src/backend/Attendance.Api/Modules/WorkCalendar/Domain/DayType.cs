namespace Attendance.Api.Modules.WorkCalendar.Domain;

/// <summary>
/// Defines the possible labor classifications of a calendar date.
/// </summary>
public enum DayType
{
    WorkingDay = 1,
    NonWorkingDay = 2,
    Holiday = 3
}