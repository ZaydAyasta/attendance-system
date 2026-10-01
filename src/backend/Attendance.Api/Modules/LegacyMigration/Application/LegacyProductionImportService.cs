using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Attendance.Domain;
using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.LegacyMigration.Contracts;
using Attendance.Api.Modules.LegacyMigration.Domain;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Api.Modules.LegacyMigration.Application;

/// <summary>
/// Applies a prevalidated legacy payload atomically. Mappings make retries safe.
/// </summary>
public sealed class LegacyProductionImportService(AttendanceDbContext dbContext)
{
    private const string SourceSystem = "legacy-supabase-excel-v1";
    private const string EmployeeEntityType = "Employee";
    private const string AttendanceMarkEntityType = "AttendanceMark";

    public async Task<LegacyProductionImportResult> ApplyAsync(
        LegacyProductionImportRequest request,
        CancellationToken cancellationToken)
    {
        var employees = ValidateEmployees(request.Employees);
        var marks = ValidateMarks(request.Marks, employees);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var employeeIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var createdEmployees = 0;
        var createdMarks = 0;
        var reconciledMappings = 0;
        var skippedMappings = 0;

        foreach (var source in employees)
        {
            var mapping = await dbContext.LegacyImportMappings.SingleOrDefaultAsync(
                item => item.SourceSystem == SourceSystem &&
                    item.EntityType == EmployeeEntityType &&
                    item.LegacyId == source.EmployeeCode,
                cancellationToken);
            if (mapping is not null)
            {
                employeeIds[source.EmployeeCode] = mapping.DestinationId;
                skippedMappings++;
                continue;
            }

            var employee = await dbContext.Employees.SingleOrDefaultAsync(
                item => item.EmployeeCode == source.EmployeeCode,
                cancellationToken);
            if (employee is null)
            {
                employee = Employee.Create(source.EmployeeCode, source.FirstName, source.LastName, source.HireDate);
                dbContext.Employees.Add(employee);
                createdEmployees++;
            }
            else if (employee.FirstName != source.FirstName || employee.LastName != source.LastName || employee.HireDate != source.HireDate)
            {
                throw new InvalidOperationException("An employee code conflicts with an existing production employee.");
            }
            else
            {
                reconciledMappings++;
            }

            employeeIds[source.EmployeeCode] = employee.Id;
            dbContext.LegacyImportMappings.Add(LegacyImportMapping.Create(
                SourceSystem,
                EmployeeEntityType,
                source.EmployeeCode,
                employee.Id,
                now));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var source in marks)
        {
            var mapping = await dbContext.LegacyImportMappings.SingleOrDefaultAsync(
                item => item.SourceSystem == SourceSystem &&
                    item.EntityType == AttendanceMarkEntityType &&
                    item.LegacyId == source.LegacyId,
                cancellationToken);
            if (mapping is not null)
            {
                skippedMappings++;
                continue;
            }

            var employeeId = employeeIds[source.EmployeeCode];
            var existing = await dbContext.AttendanceMarks.SingleOrDefaultAsync(
                item => item.EmployeeId == employeeId &&
                    item.OccurredAt == source.OccurredAt &&
                    item.Type == source.Type &&
                    item.Source == AttendanceSource.LegacyFingerprint &&
                    item.CheckpointId == null,
                cancellationToken);
            if (existing is null)
            {
                existing = AttendanceMark.Create(
                    employeeId,
                    source.OccurredAt,
                    source.Type,
                    AttendanceSource.LegacyFingerprint,
                    null);
                dbContext.AttendanceMarks.Add(existing);
                createdMarks++;
            }
            else
            {
                reconciledMappings++;
            }

            dbContext.LegacyImportMappings.Add(LegacyImportMapping.Create(
                SourceSystem,
                AttendanceMarkEntityType,
                source.LegacyId,
                existing.Id,
                now));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new LegacyProductionImportResult(createdEmployees, createdMarks, reconciledMappings, skippedMappings);
    }

    private static IReadOnlyList<NormalizedEmployee> ValidateEmployees(
        IReadOnlyList<LegacyProductionImportEmployee>? requestEmployees)
    {
        if (requestEmployees is null || requestEmployees.Count == 0)
        {
            throw new InvalidOperationException("The legacy payload must include at least one employee.");
        }

        var employees = requestEmployees.Select(item => new NormalizedEmployee(
            Required(item.EmployeeCode),
            Required(item.FirstName),
            Required(item.LastName),
            item.HireDate)).ToArray();
        if (employees.Select(item => item.EmployeeCode).Distinct(StringComparer.OrdinalIgnoreCase).Count() != employees.Length)
        {
            throw new InvalidOperationException("The legacy payload contains duplicate employee codes.");
        }

        return employees;
    }

    private static IReadOnlyList<NormalizedMark> ValidateMarks(
        IReadOnlyList<LegacyProductionImportMark>? requestMarks,
        IReadOnlyList<NormalizedEmployee> employees)
    {
        if (requestMarks is null || requestMarks.Count == 0)
        {
            throw new InvalidOperationException("The legacy payload must include at least one attendance mark.");
        }

        var employeeCodes = employees.Select(item => item.EmployeeCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var marks = requestMarks.Select(item =>
        {
            var employeeCode = Required(item.EmployeeCode);
            var legacyId = Required(item.LegacyId);
            if (!employeeCodes.Contains(employeeCode))
            {
                throw new InvalidOperationException("An attendance mark references an employee outside the legacy payload.");
            }

            if (item.OccurredAt == default ||
                !Enum.TryParse<AttendanceMarkType>(item.Type, ignoreCase: true, out var type) ||
                !Enum.IsDefined(type))
            {
                throw new InvalidOperationException("The legacy payload contains an invalid attendance mark.");
            }

            return new NormalizedMark(employeeCode, item.OccurredAt, type, legacyId);
        }).ToArray();

        if (marks.Select(item => item.LegacyId).Distinct(StringComparer.Ordinal).Count() != marks.Length)
        {
            throw new InvalidOperationException("The legacy payload contains duplicate attendance identities.");
        }

        return marks;
    }

    private static string Required(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException("The legacy payload contains a required empty value.")
            : value.Trim();

    private sealed record NormalizedEmployee(string EmployeeCode, string FirstName, string LastName, DateOnly HireDate);
    private sealed record NormalizedMark(string EmployeeCode, DateTimeOffset OccurredAt, AttendanceMarkType Type, string LegacyId);
}
