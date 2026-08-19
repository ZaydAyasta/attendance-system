using Attendance.Api.Modules.Absences.Contracts;

namespace Attendance.Api.Modules.Absences.Application;

public enum AbsenceEmployeeHistoryStatus
{
    Success,
    EmployeeNotFound
}

public sealed record AbsenceEmployeeHistoryResult(
    AbsenceEmployeeHistoryStatus Status,
    IReadOnlyList<AbsenceResponse>? Value = null);
