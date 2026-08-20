namespace Attendance.Api.Modules.Attendance.Application;

public sealed class AttendanceValidationResult<T>
{
    private AttendanceValidationResult(
        T? value,
        Dictionary<string, string[]> errors)
    {
        Value = value;
        Errors = errors;
    }

    public T? Value { get; }

    public Dictionary<string, string[]> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public static AttendanceValidationResult<T> Success(T value)
        => new(value, new Dictionary<string, string[]>());

    public static AttendanceValidationResult<T> Failure(
        Dictionary<string, string[]> errors)
        => new(default, errors);
}
