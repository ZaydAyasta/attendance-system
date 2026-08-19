using Attendance.Api.Modules.WorkCalendar.Domain;

namespace Attendance.Api.Modules.WorkCalendar.Application;

public sealed record CreateWorkCalendarDayCommand(
    DateOnly Date,
    DayType DayType,
    string? Description);

public sealed record UpdateWorkCalendarDayCommand(
    DayType DayType,
    string? Description,
    uint ExpectedVersion);
