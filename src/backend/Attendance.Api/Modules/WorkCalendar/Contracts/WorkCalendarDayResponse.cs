namespace Attendance.Api.Modules.WorkCalendar.Contracts;

public sealed record WorkCalendarDayResponse(
    DateOnly Date,
    string DayType,
    string? Description,
    uint Version);
