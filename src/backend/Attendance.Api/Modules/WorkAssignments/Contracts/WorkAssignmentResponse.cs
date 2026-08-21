namespace Attendance.Api.Modules.WorkAssignments.Contracts;

public sealed record WorkAssignmentResponse(
    Guid Id,
    Guid EmployeeId,
    DateOnly Date,
    string Type,
    string? Comment,
    string Status,
    uint Version);
