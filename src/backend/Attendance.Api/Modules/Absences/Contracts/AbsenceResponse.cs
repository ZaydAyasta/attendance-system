namespace Attendance.Api.Modules.Absences.Contracts;

public sealed record AbsenceResponse(
    Guid Id,
    Guid EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Type,
    string Status,
    string? Reason,
    string? Notes,
    uint Version);
