using Attendance.Api.Modules.WorkAssignments.Domain;

namespace Attendance.Api.Modules.WorkAssignments.Application;

public sealed record CreateWorkAssignmentCommand(
    Guid EmployeeId,
    DateOnly Date,
    WorkAssignmentType Type,
    string? Comment);

public sealed record UpdateWorkAssignmentCommand(
    DateOnly Date,
    WorkAssignmentType Type,
    string? Comment,
    uint ExpectedVersion);

public sealed record CancelWorkAssignmentCommand(uint ExpectedVersion);
