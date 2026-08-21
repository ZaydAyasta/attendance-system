namespace Attendance.Api.Modules.Attendance.Contracts;

public sealed record DailyAttendanceResponse(
    Guid EmployeeId,
    DateOnly Date,
    string? Status,
    IReadOnlyCollection<string> Anomalies,
    string? Failure,
    int? GrossMinutes,
    int? LunchMinutes,
    int? WorkedMinutes,
    bool TimeCalculationComplete,
    IReadOnlyCollection<string> TimeIssues);
