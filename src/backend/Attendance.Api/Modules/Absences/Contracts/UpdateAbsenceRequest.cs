namespace Attendance.Api.Modules.Absences.Contracts;

public sealed record UpdateAbsenceRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    string? Type,
    string? Reason,
    string? Notes,
    uint Version);
