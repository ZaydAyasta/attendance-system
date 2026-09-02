using Attendance.Api.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Attendance.LegacyImporter;

public static class LegacyImporterProgram
{
    public static async Task<int> RunAsync(
        string[] args,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        if (!TryParse(args, out var command, out var excelPath, out var apply))
        {
            await output.WriteLineAsync("Usage: Attendance.LegacyImporter <inspect|diagnose|dry-run|import|verify> --excel <file-or-folder> [--apply]");
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
        var (excelMarks, validation) = new LegacyExcelReader().Read(excelPath);
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
        if (command == "import")
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

    private static bool TryParse(string[] args, out string command, out string excelPath, out bool apply)
    {
        command = args.FirstOrDefault()?.ToLowerInvariant() ?? string.Empty;
        apply = args.Contains("--apply", StringComparer.OrdinalIgnoreCase);
        var index = Array.FindIndex(args, item => string.Equals(item, "--excel", StringComparison.OrdinalIgnoreCase));
        excelPath = index >= 0 && index + 1 < args.Length ? args[index + 1] : string.Empty;
        return new[] { "inspect", "diagnose", "dry-run", "import", "verify" }.Contains(command)
            && !string.IsNullOrWhiteSpace(excelPath)
            && ((command == "import" && apply) || (command != "import" && !apply));
    }

    private static async Task WritePlanAsync(TextWriter output, string command, LegacyImportPlan plan)
    {
        await output.WriteLineAsync($"{command}: employees={plan.Employees.Count}, marks={plan.Marks.Count}, duplicate marks collapsed={plan.DuplicateMarkOccurrences}.");
        foreach (var warning in plan.Validation.Warnings.Distinct(StringComparer.Ordinal)) await output.WriteLineAsync($"Warning: {warning}");
        foreach (var blocker in plan.Validation.Blockers.Distinct(StringComparer.Ordinal)) await output.WriteLineAsync($"Blocker: {blocker}");
    }
}
