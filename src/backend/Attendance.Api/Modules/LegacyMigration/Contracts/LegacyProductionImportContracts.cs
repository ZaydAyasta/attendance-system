namespace Attendance.Api.Modules.LegacyMigration.Contracts;

/// <summary>
/// One-time, administrator-submitted legacy payload. The route that accepts it
/// is not mapped unless LegacyImport:Enabled is explicitly enabled.
/// </summary>
public sealed record LegacyProductionImportRequest(
    IReadOnlyList<LegacyProductionImportEmployee> Employees,
    IReadOnlyList<LegacyProductionImportMark> Marks);

public sealed record LegacyProductionImportEmployee(
    string EmployeeCode,
    string FirstName,
    string LastName,
    DateOnly HireDate);

public sealed record LegacyProductionImportMark(
    string EmployeeCode,
    DateTimeOffset OccurredAt,
    string Type,
    string LegacyId);

public sealed record LegacyProductionImportResult(
    int CreatedEmployees,
    int CreatedMarks,
    int ReconciledMappings,
    int SkippedMappings);
