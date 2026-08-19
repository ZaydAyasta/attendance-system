namespace Attendance.Api.Modules.Absences.Domain;

/// <summary>
/// Represents an employee absence registered for a specific period.
/// </summary>
public sealed class Absence
{
    public const int ReasonMaxLength = 500;

    public const int NotesMaxLength = 1000;

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    public DateRange Period { get; private set; }

    public AbsenceType Type { get; private set; }

    public AbsenceStatus Status { get; private set; }

    public string? Reason { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>
    /// Gets the PostgreSQL row version used for optimistic concurrency control.
    /// </summary>
    public uint Version { get; private set; }

    private Absence()
    {
    }

    private Absence(
        Guid id,
        Guid employeeId,
        DateRange period,
        AbsenceType type,
        AbsenceStatus status,
        string? reason,
        string? notes)
    {
        Id = id;
        EmployeeId = EnsureValidEmployeeId(employeeId);
        Period = period;
        Type = EnsureValidType(type);
        Status = EnsureValidStatus(status);
        Reason = NormalizeText(reason, ReasonMaxLength, nameof(reason));
        Notes = NormalizeText(notes, NotesMaxLength, nameof(notes));
    }

    public static Absence Create(
        Guid employeeId,
        DateRange period,
        AbsenceType type,
        AbsenceStatus status,
        string? reason,
        string? notes)
        => new(
            Guid.NewGuid(),
            employeeId,
            period,
            type,
            status,
            reason,
            notes);

    public void Update(
        DateRange period,
        AbsenceType type,
        AbsenceStatus status,
        string? reason,
        string? notes)
    {
        Period = period;
        Type = EnsureValidType(type);
        Status = EnsureValidStatus(status);
        Reason = NormalizeText(reason, ReasonMaxLength, nameof(reason));
        Notes = NormalizeText(notes, NotesMaxLength, nameof(notes));
    }

    public void Cancel()
    {
        Status = AbsenceStatus.Cancelled;
    }

    private static Guid EnsureValidEmployeeId(Guid employeeId)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "EmployeeId must be a non-empty GUID.",
                nameof(employeeId));
        }

        return employeeId;
    }

    private static AbsenceType EnsureValidType(AbsenceType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Unsupported absence type.");
        }

        return type;
    }

    private static AbsenceStatus EnsureValidStatus(AbsenceStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unsupported absence status.");
        }

        return status;
    }

    private static string? NormalizeText(
        string? value,
        int maxLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }
}
