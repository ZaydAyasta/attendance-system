namespace Attendance.Api.Modules.WorkCalendar.Application;

public enum WorkCalendarWriteStatus
{
    Success,
    NotFound,
    Duplicate,
    ConcurrencyConflict
}

public sealed record WorkCalendarWriteResult<T>(
    WorkCalendarWriteStatus Status,
    T? Value = default);

public sealed record WorkCalendarWriteResult(
    WorkCalendarWriteStatus Status);
