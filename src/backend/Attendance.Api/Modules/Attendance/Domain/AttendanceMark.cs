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
}