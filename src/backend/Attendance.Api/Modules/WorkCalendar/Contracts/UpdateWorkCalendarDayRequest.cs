namespace Attendance.Api.Modules.WorkCalendar.Contracts;

public sealed record UpdateWorkCalendarDayRequest(
    string? DayType,
    string? Description,
    uint Version);
