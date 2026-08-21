namespace Attendance.Api.Modules.WorkAssignments.Application;

public enum WorkAssignmentWriteStatus
{
    Success,
    NotFound,
    EmployeeNotFound,
    EmployeeInactive,
    InvalidState,
    DuplicateActiveAssignment,
    HolidayConflict,
    ConcurrencyConflict
}

public sealed record WorkAssignmentWriteResult<T>(
    WorkAssignmentWriteStatus Status,
    T? Value = default);

public sealed record WorkAssignmentWriteResult(WorkAssignmentWriteStatus Status);
