using Attendance.Api.Modules.WorkCalendar.Domain;

namespace Attendance.Api.Modules.WorkCalendar.Application;

public sealed record BulkConfigureWorkCalendarCommand(
    IReadOnlyList<BulkConfigureWorkCalendarDayCommand> Days,
    bool OverwriteExisting);

public sealed record BulkConfigureWorkCalendarDayCommand(
    DateOnly Date, DayType DayType, string? Description, uint? ExpectedVersion);
