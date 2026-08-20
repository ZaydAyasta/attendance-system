namespace Attendance.Api.Modules.Attendance.Contracts;

public sealed record EmployeeAttendanceRangeResponse(
    Guid EmployeeId,
    DateOnly From,
    DateOnly To,
    IReadOnlyCollection<DailyAttendanceResponse> Days);
