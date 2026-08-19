namespace Attendance.Api.Modules.WorkCalendar.Application;

public sealed class WorkCalendarValidationResult<T>
{
    private WorkCalendarValidationResult(
        T? value,
        Dictionary<string, string[]> errors)
    {
        Value = value;
        Errors = errors;
    }

    public T? Value { get; }

    public Dictionary<string, string[]> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public static WorkCalendarValidationResult<T> Success(T value)
        => new(value, new Dictionary<string, string[]>());

    public static WorkCalendarValidationResult<T> Failure(
        Dictionary<string, string[]> errors)
        => new(default, errors);
}
