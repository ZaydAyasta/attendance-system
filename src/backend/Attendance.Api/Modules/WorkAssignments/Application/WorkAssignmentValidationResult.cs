namespace Attendance.Api.Modules.WorkAssignments.Application;

public sealed class WorkAssignmentValidationResult<T>
{
    private WorkAssignmentValidationResult(
        T? value,
        Dictionary<string, string[]> errors)
    {
        Value = value;
        Errors = errors;
    }

    public T? Value { get; }

    public Dictionary<string, string[]> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public static WorkAssignmentValidationResult<T> Success(T value)
        => new(value, new Dictionary<string, string[]>());

    public static WorkAssignmentValidationResult<T> Failure(
        Dictionary<string, string[]> errors)
        => new(default, errors);
}
