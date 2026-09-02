using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Attendance.Domain;
using Microsoft.EntityFrameworkCore;

namespace Attendance.LegacyImporter;

public sealed class LegacyDestinationVerifier(DbContextOptions<AttendanceDbContext> options)
{
    public async Task<LegacyValidationReport> VerifyAsync(LegacyImportPlan plan, CancellationToken cancellationToken)
    {
        var report = new LegacyValidationReport();
        await using var db = new AttendanceDbContext(options);
        var mappings = await db.LegacyImportMappings.AsNoTracking()
            .Where(x => x.SourceSystem == LegacyImportConstants.SourceSystem)
            .ToListAsync(cancellationToken);
        var employeeMappings = mappings.Where(x => x.EntityType == LegacyImportConstants.EmployeeEntityType).ToDictionary(x => x.LegacyId, StringComparer.Ordinal);
        var markMappings = mappings.Where(x => x.EntityType == LegacyImportConstants.AttendanceMarkEntityType).ToDictionary(x => x.LegacyId, StringComparer.Ordinal);

        foreach (var employee in plan.Employees)
        {
            if (!employeeMappings.TryGetValue(employee.EmployeeCode, out var mapping)) { report.Blockers.Add("An expected employee mapping is missing."); continue; }
            var exists = await db.Employees.AsNoTracking().AnyAsync(x => x.Id == mapping.DestinationId && x.EmployeeCode == employee.EmployeeCode, cancellationToken);
            if (!exists) report.Blockers.Add("An employee mapping is orphaned or mismatched.");
        }
        foreach (var mark in plan.Marks)
        {
            if (!markMappings.TryGetValue(mark.LegacyId, out var mapping)) { report.Blockers.Add("An expected attendance mark mapping is missing."); continue; }
            var exists = await db.AttendanceMarks.AsNoTracking().AnyAsync(x => x.Id == mapping.DestinationId && x.Source == AttendanceSource.LegacyFingerprint && x.CheckpointId == null, cancellationToken);
            if (!exists) report.Blockers.Add("An attendance mark mapping is orphaned or has an invalid source/checkpoint.");
        }
        if (mappings.GroupBy(x => (x.EntityType, x.LegacyId)).Any(group => group.Count() > 1)) report.Blockers.Add("Duplicate legacy mappings were found.");
        return report;
    }
}
