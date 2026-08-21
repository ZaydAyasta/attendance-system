namespace Attendance.Api.Modules.WorkAssignments.Contracts;

public sealed record UpdateWorkAssignmentRequest(
    DateOnly Date,
    string Type,
    string? Comment,
    uint Version);
