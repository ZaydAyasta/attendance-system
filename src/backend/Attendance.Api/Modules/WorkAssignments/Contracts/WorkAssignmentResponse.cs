namespace Attendance.Api.Modules.WorkAssignments.Contracts;

public sealed record WorkAssignmentResponse(
    Guid Id,
    Guid EmployeeId,
    WorkAssignmentEmployeeSummaryResponse Employee,
    DateOnly Date,
    string Type,
    string? Comment,
    string Status,
    uint Version);
