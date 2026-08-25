namespace Attendance.Api.Modules.WorkCalendar.Contracts;

public sealed record BulkConfigureWorkCalendarRequest(
    IReadOnlyList<BulkConfigureWorkCalendarDayRequest>? Days,
    bool OverwriteExisting);

public sealed record BulkConfigureWorkCalendarDayRequest(
    DateOnly Date,
    string? DayType,
    string? Description,
    uint? Version);

public sealed record BulkConfigureWorkCalendarResponse(int Created, int Updated, int Skipped);
