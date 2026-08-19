namespace Attendance.Api.Modules.Absences.Domain;

/// <summary>
/// Defines the administrative status of an absence.
/// </summary>
public enum AbsenceStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}