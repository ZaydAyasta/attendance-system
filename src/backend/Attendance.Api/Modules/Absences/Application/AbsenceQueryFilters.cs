using Attendance.Api.Modules.Absences.Domain;

namespace Attendance.Api.Modules.Absences.Application;

public sealed record AbsenceQueryFilters(
    Guid? EmployeeId,
    DateOnly? From,
    DateOnly? To,
    AbsenceStatus? Status,
    AbsenceType? Type);
