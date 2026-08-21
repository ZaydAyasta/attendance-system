using Attendance.Api.Modules.Absences.Domain;
using Attendance.Api.Modules.Attendance.Domain;
using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.WorkCalendar.Domain;
using Attendance.Api.Modules.WorkAssignments.Domain;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Api.BuildingBlocks.Persistence;

/// <summary>
/// Represents the Entity Framework Core database context
/// used by the attendance system.
/// </summary>
public sealed class AttendanceDbContext(
    DbContextOptions<AttendanceDbContext> options)
    : DbContext(options)
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AttendanceDbContext).Assembly);
    }
}
