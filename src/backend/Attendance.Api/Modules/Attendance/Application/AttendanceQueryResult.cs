namespace Attendance.Api.Modules.Attendance.Application;

public enum AttendanceQueryStatus
{
    Success,
    EmployeeNotFound
}

public sealed record AttendanceQueryResult<T>(
    AttendanceQueryStatus Status,
    T? Value = default);
