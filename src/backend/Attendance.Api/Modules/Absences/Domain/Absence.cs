namespace Attendance.Api.Modules.Absences.Domain;

/// <summary>
/// Represents an employee absence registered for a specific period.
/// </summary>
public sealed class Absence
{
    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    public DateRange Period { get; private set; }

    public AbsenceType Type { get; private set; }

    public AbsenceStatus Status { get; private set; }

    public string? Reason { get; private set; }

    public string? Notes { get; private set; }
}