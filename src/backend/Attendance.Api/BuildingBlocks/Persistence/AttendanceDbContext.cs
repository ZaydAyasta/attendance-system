using Attendance.Api.Modules.Absences.Domain;
using Attendance.Api.Modules.Attendance.Domain;
using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.WorkCalendar.Domain;
using Attendance.Api.Modules.WorkAssignments.Domain;
using Attendance.Api.Modules.Identity.Domain;
using Attendance.Api.Modules.Checkpoints.Domain;
using Attendance.Api.Modules.LegacyMigration.Domain;
using Attendance.Api.Modules.Auditing.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Api.BuildingBlocks.Persistence;

/// <summary>
/// Represents the Entity Framework Core database context
/// used by the attendance system.
/// </summary>
public sealed class AttendanceDbContext(
    DbContextOptions<AttendanceDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<WorkCalendarDay> WorkCalendarDays =>
        Set<WorkCalendarDay>();

    public DbSet<Absence> Absences =>
        Set<Absence>();

    public DbSet<AttendanceMark> AttendanceMarks =>
        Set<AttendanceMark>();

    public DbSet<EmployeeWorkAssignment> EmployeeWorkAssignments =>
        Set<EmployeeWorkAssignment>();

    public DbSet<Checkpoint> Checkpoints => Set<Checkpoint>();

    public DbSet<LegacyImportMapping> LegacyImportMappings => Set<LegacyImportMapping>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AttendanceDbContext).Assembly);
    }
}
