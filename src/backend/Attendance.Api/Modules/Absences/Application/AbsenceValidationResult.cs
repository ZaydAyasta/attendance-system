namespace Attendance.Api.Modules.Absences.Application;

public sealed class AbsenceValidationResult<T>
{
    private AbsenceValidationResult(
        T? value,
        Dictionary<string, string[]> errors)
    {
        Value = value;
        Errors = errors;
    }

    public T? Value { get; }

    public Dictionary<string, string[]> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public static AbsenceValidationResult<T> Success(T value)
        => new(value, new Dictionary<string, string[]>());

    public static AbsenceValidationResult<T> Failure(
        Dictionary<string, string[]> errors)
        => new(default, errors);
}
