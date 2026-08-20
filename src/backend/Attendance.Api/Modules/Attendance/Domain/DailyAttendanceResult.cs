namespace Attendance.Api.Modules.Attendance.Domain;

/// <summary>
/// Represents the evaluator output for one employee and one date.
/// </summary>
public sealed class DailyAttendanceResult
{
    private DailyAttendanceResult(
        Guid employeeId,
        DateOnly date,
        AttendanceStatus? status,
        IReadOnlyCollection<AttendanceAnomaly> anomalies,
        AttendanceEvaluationFailure? failure)
    {
        EmployeeId = employeeId;
        Date = date;
        Status = status;
        Anomalies = anomalies;
        Failure = failure;
    }

    public Guid EmployeeId { get; }

    public DateOnly Date { get; }

    public AttendanceStatus? Status { get; }

    public IReadOnlyCollection<AttendanceAnomaly> Anomalies { get; }

    public AttendanceEvaluationFailure? Failure { get; }

    public bool IsSuccess => Failure is null;

    public static DailyAttendanceResult Success(
        Guid employeeId,
        DateOnly date,
        AttendanceStatus status,
        params AttendanceAnomaly[] anomalies)
        => new(
            employeeId,
            date,
            status,
            anomalies,
            null);

    public static DailyAttendanceResult FailureResult(
        Guid employeeId,
        DateOnly date,
        AttendanceEvaluationFailure failure)
        => new(
            employeeId,
            date,
            null,
            Array.Empty<AttendanceAnomaly>(),
            failure);
}
