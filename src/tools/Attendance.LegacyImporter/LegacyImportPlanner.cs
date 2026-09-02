namespace Attendance.LegacyImporter;

public sealed class LegacyImportPlanner
{
    public LegacyImportPlan CreatePlan(
        IReadOnlyList<LegacyEmployeeSourceRow> sourceRows,
        IReadOnlyList<LegacyExcelMark> excelMarks,
        LegacyValidationReport validation)
    {
        if (sourceRows.Count == 0)
        {
            validation.Blockers.Add("No employees are visible to legacy_migration_reader. Check the approved RLS SELECT policies.");
        }

        var employees = new List<LegacyEmployee>();
        var names = new Dictionary<string, List<LegacyEmployee>>(StringComparer.Ordinal);
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in sourceRows)
        {
            if (string.IsNullOrWhiteSpace(row.EmployeeCode))
            {
                validation.Blockers.Add("Supabase employee_profiles.employee_key is required.");
                continue;
            }
            if (!codes.Add(row.EmployeeCode.Trim()))
            {
                validation.Blockers.Add($"Duplicate employee key '{row.EmployeeCode}'.");
                continue;
            }
            if (row.HireDate is null)
            {
                validation.Blockers.Add($"Employee '{row.EmployeeCode}' has no employees.hire_date.");
                continue;
            }
            if (!LegacyText.TrySplitFullName(row.EmployeeName, out var firstName, out var lastName))
            {
                validation.Blockers.Add($"Employee '{row.EmployeeCode}' does not have a composable full name.");
                continue;
            }

            var employee = new LegacyEmployee(row.EmployeeCode.Trim(), firstName, lastName, row.HireDate.Value, LegacyText.NormalizeName(row.EmployeeName));
            employees.Add(employee);
            if (!names.TryGetValue(employee.NormalizedName, out var matches))
            {
                matches = [];
                names[employee.NormalizedName] = matches;
            }
            matches.Add(employee);
        }

        var resolved = new List<ResolvedLegacyMark>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var duplicateCount = 0;
        foreach (var mark in excelMarks)
        {
            if (!names.TryGetValue(mark.NormalizedEmployeeName, out var matches))
            {
                validation.Blockers.Add("An Excel employee has no exact Supabase employee match.");
                continue;
            }
            if (matches.Count != 1)
            {
                validation.Blockers.Add("An Excel employee matches multiple Supabase employees.");
                continue;
            }

            var employee = matches[0];
            var legacyId = LegacyText.CreateMarkLegacyId(employee.EmployeeCode, mark.LocalDate, mark.LocalTime, mark.Type.ToString());
            if (!ids.Add(legacyId))
            {
                duplicateCount++;
                continue;
            }
            resolved.Add(new ResolvedLegacyMark(mark, employee, legacyId));
        }

        return new LegacyImportPlan(employees, resolved, validation, duplicateCount);
    }
}
