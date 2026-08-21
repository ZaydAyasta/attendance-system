namespace Attendance.Api.Modules.WorkAssignments.Contracts;

public sealed record CreateWorkAssignmentRequest(
    Guid EmployeeId,
    DateOnly Date,
    string Type,
    string? Comment);
