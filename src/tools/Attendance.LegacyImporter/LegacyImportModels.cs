using Attendance.Api.Modules.Attendance.Domain;

namespace Attendance.LegacyImporter;

public sealed record LegacyEmployeeSourceRow(string EmployeeCode, string EmployeeName, DateOnly? HireDate);

public sealed record LegacyEmployee(string EmployeeCode, string FirstName, string LastName, DateOnly HireDate, string NormalizedName);

public sealed record LegacyExcelMark(
    string EmployeeName,
    string NormalizedEmployeeName,
    DateOnly LocalDate,
    TimeOnly LocalTime,
    AttendanceMarkType Type,
    string FilePath,
    string Worksheet,
    int RowNumber);

public sealed record ResolvedLegacyMark(LegacyExcelMark Source, LegacyEmployee Employee, string LegacyId)
{
    public DateTimeOffset OccurredAt => LegacyTime.ToUtc(Source.LocalDate, Source.LocalTime);
}

public sealed class LegacyValidationReport
{
    public List<string> Blockers { get; } = [];
    public List<string> Warnings { get; } = [];
    public bool IsValid => Blockers.Count == 0;
}

public sealed record LegacyImportPlan(
    IReadOnlyList<LegacyEmployee> Employees,
    IReadOnlyList<ResolvedLegacyMark> Marks,
    LegacyValidationReport Validation,
    int DuplicateMarkOccurrences);

public static class LegacyImportConstants
{
    public const string SourceSystem = "legacy-supabase-excel-v1";
    public const string EmployeeEntityType = "Employee";
    public const string AttendanceMarkEntityType = "AttendanceMark";
}
