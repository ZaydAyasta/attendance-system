namespace Attendance.Api.Modules.Attendance.Application;

public sealed record DailyAttendanceQuery(DateOnly Date);

public sealed record AttendanceRangeQuery(DateOnly From, DateOnly To);
