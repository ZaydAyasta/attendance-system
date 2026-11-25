namespace Attendance.Api.Modules.Attendance.Contracts;

public sealed record AttendanceMarkChange(DateOnly? Date, TimeOnly? Time, string? Type, string? Reason);

public sealed record AttendanceMarkHistoryItem(
    Guid Id, DateTimeOffset OccurredAt, TimeOnly LocalTime, string Type, string Source,
    string? ManualReason, DateTimeOffset? VoidedAt, string? VoidReason, Guid? ReplacesMarkId);
