using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Attendance.Domain;
using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.LegacyMigration.Domain;
using Microsoft.EntityFrameworkCore;

namespace Attendance.LegacyImporter;

public sealed record LegacyApplyResult(int CreatedEmployees, int CreatedMarks, int ReconciledMappings, int SkippedMappings);

public sealed class LegacyDestinationImporter(DbContextOptions<AttendanceDbContext> options)
{
    public async Task<LegacyApplyResult> ApplyAsync(LegacyImportPlan plan, CancellationToken cancellationToken)
    {
        if (!plan.Validation.IsValid) throw new InvalidOperationException("Import cannot apply while validation blockers exist.");
        await using var db = new AttendanceDbContext(options);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var employeeIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var createdEmployees = 0;
        var reconciled = 0;
        var skipped = 0;

        foreach (var source in plan.Employees)
        {
            var mapping = await db.LegacyImportMappings.SingleOrDefaultAsync(x => x.SourceSystem == LegacyImportConstants.SourceSystem && x.EntityType == LegacyImportConstants.EmployeeEntityType && x.LegacyId == source.EmployeeCode, cancellationToken);
            if (mapping is not null) { employeeIds[source.EmployeeCode] = mapping.DestinationId; skipped++; continue; }
            var existing = await db.Employees.SingleOrDefaultAsync(x => x.EmployeeCode == source.EmployeeCode, cancellationToken);
            if (existing is null)
            {
                existing = Employee.Create(source.EmployeeCode, source.FirstName, source.LastName, source.HireDate);
                db.Employees.Add(existing);
                createdEmployees++;
            }
            else if (existing.FirstName != source.FirstName || existing.LastName != source.LastName || existing.HireDate != source.HireDate)
            {
                throw new InvalidOperationException($"Employee code conflict for '{source.EmployeeCode}'.");
            }
            else { reconciled++; }
            employeeIds[source.EmployeeCode] = existing.Id;
            db.LegacyImportMappings.Add(LegacyImportMapping.Create(LegacyImportConstants.SourceSystem, LegacyImportConstants.EmployeeEntityType, source.EmployeeCode, existing.Id, now));
        }

        await db.SaveChangesAsync(cancellationToken);
        var createdMarks = 0;
        foreach (var source in plan.Marks)
        {
            var mapping = await db.LegacyImportMappings.SingleOrDefaultAsync(x => x.SourceSystem == LegacyImportConstants.SourceSystem && x.EntityType == LegacyImportConstants.AttendanceMarkEntityType && x.LegacyId == source.LegacyId, cancellationToken);
            if (mapping is not null) { skipped++; continue; }
            var employeeId = employeeIds[source.Employee.EmployeeCode];
            var existing = await db.AttendanceMarks.SingleOrDefaultAsync(x => x.EmployeeId == employeeId && x.OccurredAt == source.OccurredAt && x.Type == source.Source.Type && x.Source == AttendanceSource.LegacyFingerprint && x.CheckpointId == null, cancellationToken);
            if (existing is null)
            {
                existing = AttendanceMark.Create(employeeId, source.OccurredAt, source.Source.Type, AttendanceSource.LegacyFingerprint, null);
                db.AttendanceMarks.Add(existing);
                createdMarks++;
            }
            else { reconciled++; }
            db.LegacyImportMappings.Add(LegacyImportMapping.Create(LegacyImportConstants.SourceSystem, LegacyImportConstants.AttendanceMarkEntityType, source.LegacyId, existing.Id, now));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new LegacyApplyResult(createdEmployees, createdMarks, reconciled, skipped);
    }
}
