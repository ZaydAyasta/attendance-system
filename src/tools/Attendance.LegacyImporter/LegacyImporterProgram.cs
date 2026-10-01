using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.LegacyMigration.Contracts;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;

namespace Attendance.LegacyImporter;

public static class LegacyImporterProgram
{
    public static async Task<int> RunAsync(
        string[] args,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        if (!TryParse(args, out var command, out var excelPath, out var apply, out var excludedNames, out var excludedEmployeeCodes, out var outputPath))
        {
            await output.WriteLineAsync("Usage: Attendance.LegacyImporter <inspect|diagnose|dry-run|export-payload|import|verify> --excel <file-or-folder> [--exclude-employee <full-name>] [--exclude-employee-code <code>] [--output <file>] [--apply]");
            return 64;
        }

        LegacyConnectionSettings settings;
        try
        {
            settings = LegacyConnectionSettings.FromEnvironment();
        }
        catch (InvalidOperationException exception)
        {
            await output.WriteLineAsync($"Configuration error: {exception.Message}");
            return 64;
        }

        await using var source = new NpgsqlConnection(settings.SourceConnectionString);
        await source.OpenAsync(cancellationToken);

        try
        {
            await LegacySourceSafetyGuard.EnsureSafeAsync(source, settings.Destination, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            await output.WriteLineAsync($"Safety guard rejected the operation: {exception.Message}");
            return 65;
        }

        await output.WriteLineAsync($"Source: {settings.Source.DisplayName}");
        await output.WriteLineAsync($"Destination: {settings.Destination.DisplayName}");
        if (command == "inspect")
        {
            var discovery = new LegacySchemaDiscoveryService();
            var report = await discovery.InspectAsync(source, cancellationToken);
            await output.WriteLineAsync($"Current user: {report.CurrentUser}");
            await output.WriteLineAsync($"Excel path: {Path.GetFullPath(excelPath)}");
            foreach (var table in report.Tables) await output.WriteLineAsync($"- {table.Schema}.{table.Name}: {table.ColumnCount} columns");
        }

        var sourceRows = await new LegacyEmployeeSourceReader().ReadAsync(source, cancellationToken);
        if (excludedEmployeeCodes.Count > 0)
        {
            var originalCount = sourceRows.Count;
            sourceRows = sourceRows
                .Where(row => !excludedEmployeeCodes.Contains(row.EmployeeCode.Trim()))
                .ToArray();
            var excludedCount = originalCount - sourceRows.Count;
            if (excludedCount > 0)
            {
                await output.WriteLineAsync($"Excluded {excludedCount} employee configured by operator.");
            }
        }
        var (excelMarks, validation) = new LegacyExcelReader().Read(excelPath);
        if (excludedNames.Count > 0)
        {
            var originalCount = excelMarks.Count;
            excelMarks = excelMarks.Where(mark => !excludedNames.Contains(mark.NormalizedEmployeeName)).ToArray();
            var excludedCount = originalCount - excelMarks.Count;
            if (excludedCount > 0)
            {
                validation.Warnings.Add($"Excluded {excludedCount} attendance marks configured by operator.");
            }
        }
        if (command == "diagnose")
        {
            foreach (var mismatch in LegacyMismatchDiagnostics.Find(sourceRows, excelMarks))
            {
                await output.WriteLineAsync($"Excel normalized name: {mismatch.ExcelNormalizedName}");
                foreach (var candidate in mismatch.Candidates)
                {
                    await output.WriteLineAsync($"Candidate: {candidate.SupabaseNormalizedName}; shared tokens={candidate.SharedTokens}; edit distance={candidate.EditDistance}; difference={candidate.Difference}.");
                }
            }
            return 0;
        }
        var plan = new LegacyImportPlanner().CreatePlan(sourceRows, excelMarks, validation);
        await WritePlanAsync(output, command, plan);
        if (!plan.Validation.IsValid) return 2;
        if (command == "export-payload")
        {
            var payload = new LegacyProductionImportRequest(
                plan.Employees.Select(employee => new LegacyProductionImportEmployee(
                    employee.EmployeeCode,
                    employee.FirstName,
                    employee.LastName,
                    employee.HireDate)).ToArray(),
                plan.Marks.Select(mark => new LegacyProductionImportMark(
                    mark.Employee.EmployeeCode,
                    mark.OccurredAt,
                    mark.Source.Type.ToString(),
                    mark.LegacyId)).ToArray());
            await File.WriteAllTextAsync(
                outputPath,
                JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                cancellationToken);
            await output.WriteLineAsync($"Legacy import payload written to: {Path.GetFullPath(outputPath)}");
        }
        else if (command == "import")
        {
            var options = new DbContextOptionsBuilder<AttendanceDbContext>().UseNpgsql(settings.DestinationConnectionString).Options;
            var result = await new LegacyDestinationImporter(options).ApplyAsync(plan, cancellationToken);
            await output.WriteLineAsync($"Applied: employees created={result.CreatedEmployees}, marks created={result.CreatedMarks}, mappings reconciled={result.ReconciledMappings}, mappings skipped={result.SkippedMappings}.");
        }
        else if (command == "verify")
        {
            var options = new DbContextOptionsBuilder<AttendanceDbContext>().UseNpgsql(settings.DestinationConnectionString).Options;
            var verification = await new LegacyDestinationVerifier(options).VerifyAsync(plan, cancellationToken);
            foreach (var blocker in verification.Blockers.Distinct(StringComparer.Ordinal)) await output.WriteLineAsync($"Verification blocker: {blocker}");
            if (!verification.IsValid) return 2;
            await output.WriteLineAsync("Verification passed. A rerun will create zero records because every expected mapping exists.");
        }
        return 0;
    }

    private static bool TryParse(
        string[] args,
        out string command,
        out string excelPath,
        out bool apply,
        out IReadOnlySet<string> excludedNames,
        out IReadOnlySet<string> excludedEmployeeCodes,
        out string outputPath)
    {
        command = args.FirstOrDefault()?.ToLowerInvariant() ?? string.Empty;
        apply = args.Contains("--apply", StringComparer.OrdinalIgnoreCase);
        var index = Array.FindIndex(args, item => string.Equals(item, "--excel", StringComparison.OrdinalIgnoreCase));
        excelPath = index >= 0 && index + 1 < args.Length ? args[index + 1] : string.Empty;
        var outputIndex = Array.FindIndex(args, item => string.Equals(item, "--output", StringComparison.OrdinalIgnoreCase));
        outputPath = outputIndex >= 0 && outputIndex + 1 < args.Length ? args[outputIndex + 1] : string.Empty;
        excludedNames = args
            .Select((value, position) => new { value, position })
            .Where(item => string.Equals(item.value, "--exclude-employee", StringComparison.OrdinalIgnoreCase) && item.position + 1 < args.Length)
            .Select(item => LegacyText.NormalizeName(args[item.position + 1]))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.Ordinal);
        excludedEmployeeCodes = args
            .Select((value, position) => new { value, position })
            .Where(item => string.Equals(item.value, "--exclude-employee-code", StringComparison.OrdinalIgnoreCase) && item.position + 1 < args.Length)
            .Select(item => args[item.position + 1].Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new[] { "inspect", "diagnose", "dry-run", "export-payload", "import", "verify" }.Contains(command)
            && !string.IsNullOrWhiteSpace(excelPath)
            && ((command == "import" && apply) || (command != "import" && !apply))
            && (command != "export-payload" || !string.IsNullOrWhiteSpace(outputPath));
    }

    private static async Task WritePlanAsync(TextWriter output, string command, LegacyImportPlan plan)
    {
        await output.WriteLineAsync($"{command}: employees={plan.Employees.Count}, marks={plan.Marks.Count}, duplicate marks collapsed={plan.DuplicateMarkOccurrences}.");
        foreach (var warning in plan.Validation.Warnings.Distinct(StringComparer.Ordinal)) await output.WriteLineAsync($"Warning: {warning}");
        foreach (var blocker in plan.Validation.Blockers.Distinct(StringComparer.Ordinal)) await output.WriteLineAsync($"Blocker: {blocker}");
    }
}
