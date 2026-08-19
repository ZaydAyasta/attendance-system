namespace Attendance.Api.Modules.Absences.Application;

public enum AbsenceWriteStatus
{
    Success,
    NotFound,
    EmployeeNotFound,
    EmployeeInactive,
    OverlapConflict,
    ConcurrencyConflict
}

public sealed record AbsenceWriteResult<T>(
    AbsenceWriteStatus Status,
    T? Value = default);

public sealed record AbsenceWriteResult(AbsenceWriteStatus Status);
