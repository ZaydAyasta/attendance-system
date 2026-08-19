namespace Attendance.Api.Modules.WorkCalendar.Contracts;

public sealed record CreateWorkCalendarDayRequest(
    DateOnly Date,
    string? DayType,
    string? Description);
