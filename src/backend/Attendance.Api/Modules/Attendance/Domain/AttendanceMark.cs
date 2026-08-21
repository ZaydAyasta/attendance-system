namespace Attendance.Api.Modules.Attendance.Domain;

/// <summary>
/// Represents a raw attendance event captured for an employee.
/// </summary>
public sealed class AttendanceMark
{
    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Gets the date and time at which the attendance event occurred.
    /// </summary>
    public DateTimeOffset OccurredAt { get; private set; }

    public AttendanceMarkType Type { get; private set; }

    public AttendanceSource Source { get; private set; }

    /// <summary>
    /// Gets the checkpoint that produced the mark, when applicable.
    /// </summary>
    public Guid? CheckpointId { get; private set; }

    private AttendanceMark()
    {
    }

    private AttendanceMark(
        Guid id,
        Guid employeeId,
        DateTimeOffset occurredAt,
        AttendanceMarkType type,
        AttendanceSource source,
        Guid? checkpointId)
    {
        Id = id;
        EmployeeId = EnsureValidEmployeeId(employeeId);
        OccurredAt = EnsureValidOccurredAt(occurredAt);
        Type = EnsureValidType(type);
        Source = EnsureValidSource(source);
        CheckpointId = checkpointId;
    }

    public static AttendanceMark Create(
        Guid employeeId,
        DateTimeOffset occurredAt,
        AttendanceMarkType type,
        AttendanceSource source,
        Guid? checkpointId)
        => new(
            Guid.NewGuid(),
            employeeId,
            occurredAt,
            type,
            source,
            checkpointId);

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

    private static DateTimeOffset EnsureValidOccurredAt(DateTimeOffset occurredAt)
    {
        if (occurredAt == default)
        {
            throw new ArgumentException(
                "OccurredAt must be a non-default value.",
                nameof(occurredAt));
        }

        return occurredAt;
    }

    private static AttendanceMarkType EnsureValidType(AttendanceMarkType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Unsupported attendance mark type.");
        }

        return type;
    }

    private static AttendanceSource EnsureValidSource(AttendanceSource source)
    {
        if (!Enum.IsDefined(source))
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Unsupported attendance source.");
        }

        return source;
    }
}
