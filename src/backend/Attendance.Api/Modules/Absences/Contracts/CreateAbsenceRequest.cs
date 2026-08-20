namespace Attendance.Api.Modules.Absences.Contracts;

public sealed record CreateAbsenceRequest(
    Guid EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Type,
    string? Reason,
    string? Notes);
