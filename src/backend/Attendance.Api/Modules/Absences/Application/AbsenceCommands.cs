using Attendance.Api.Modules.Absences.Domain;

namespace Attendance.Api.Modules.Absences.Application;

public sealed record CreateAbsenceCommand(
    Guid EmployeeId,
    DateRange Period,
    AbsenceType Type,
    string? Reason,
    string? Notes);

public sealed record UpdateAbsenceCommand(
    DateRange Period,
    AbsenceType Type,
    string? Reason,
    string? Notes,
    uint ExpectedVersion);

public sealed record CancelAbsenceCommand(uint ExpectedVersion);
