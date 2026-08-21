using Attendance.Api.Modules.WorkAssignments.Contracts;

namespace Attendance.Api.Modules.WorkAssignments.Application;

public enum WorkAssignmentEmployeeHistoryStatus
{
    Success,
    EmployeeNotFound
}

public sealed record WorkAssignmentEmployeeHistoryResult(
    WorkAssignmentEmployeeHistoryStatus Status,
    IReadOnlyList<WorkAssignmentResponse>? Value = null);
