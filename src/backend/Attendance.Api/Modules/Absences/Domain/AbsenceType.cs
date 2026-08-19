namespace Attendance.Api.Modules.Absences.Domain;

/// <summary>
/// Defines the supported types of employee absence.
/// </summary>
public enum AbsenceType
{
    Vacation = 1,
    MedicalLeave = 2,
    Commission = 3,
    JustifiedAbsence = 4,
    Permission = 5
}