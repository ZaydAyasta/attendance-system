namespace Attendance.LegacyImporter;

public sealed record LegacyMismatchDiagnostic(string ExcelNormalizedName, IReadOnlyList<LegacyCandidate> Candidates);
public sealed record LegacyCandidate(string SupabaseName, string SupabaseNormalizedName, int SharedTokens, int EditDistance, string Difference);

public static class LegacyMismatchDiagnostics
{
    public static IReadOnlyList<LegacyMismatchDiagnostic> Find(
        IReadOnlyList<LegacyEmployeeSourceRow> sourceRows,
        IReadOnlyList<LegacyExcelMark> excelMarks)
    {
        var sourceNames = sourceRows
            .Where(row => !string.IsNullOrWhiteSpace(row.EmployeeName))
            .Select(row => (Original: row.EmployeeName.Trim(), Normalized: LegacyText.NormalizeName(row.EmployeeName)))
            .Distinct()
            .ToArray();

        return excelMarks.Select(mark => mark.NormalizedEmployeeName).Distinct(StringComparer.Ordinal)
            .Where(excel => sourceNames.All(source => source.Normalized != excel))
            .Select(excel => new LegacyMismatchDiagnostic(
                excel,
                sourceNames.Select(source => Candidate(excel, source.Original, source.Normalized))
                    .OrderByDescending(candidate => candidate.SharedTokens)
                    .ThenBy(candidate => candidate.EditDistance)
                    .Take(3)
                    .ToArray()))
            .ToArray();
    }

    private static LegacyCandidate Candidate(string excel, string original, string normalized)
    {
        var excelTokens = excel.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var sourceTokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var shared = excelTokens.Intersect(sourceTokens).Count();
        var difference = shared == 0 ? "sin palabras compartidas" : shared == Math.Min(excelTokens.Count, sourceTokens.Count) ? "apellido o nombre adicional/faltante" : "diferencia de palabras, orden o escritura";
        return new LegacyCandidate(original, normalized, shared, EditDistance(excel, normalized), difference);
    }

    private static int EditDistance(string left, string right)
    {
        var costs = Enumerable.Range(0, right.Length + 1).ToArray();
        for (var i = 1; i <= left.Length; i++)
        {
            var diagonal = costs[0]++;
            for (var j = 1; j <= right.Length; j++)
            {
                var above = costs[j];
                costs[j] = Math.Min(Math.Min(costs[j] + 1, costs[j - 1] + 1), diagonal + (left[i - 1] == right[j - 1] ? 0 : 1));
                diagonal = above;
            }
        }
        return costs[^1];
    }
}
