using Attendance.Api.Modules.WorkAssignments.Domain;

namespace Attendance.Api.Modules.WorkAssignments.Application;

public sealed record WorkAssignmentQueryFilters(
    Guid? EmployeeId,
    DateOnly? From,
    DateOnly? To,
    WorkAssignmentStatus? Status,
    WorkAssignmentType? Type);
