using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.WorkAssignments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Api.BuildingBlocks.Persistence.Configurations;

/// <summary>
/// Defines the database mapping for <see cref="EmployeeWorkAssignment"/>.
/// </summary>
public sealed class EmployeeWorkAssignmentConfiguration
    : IEntityTypeConfiguration<EmployeeWorkAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeWorkAssignment> builder)
    {
        builder.ToTable("employee_work_assignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.EmployeeId)
            .HasColumnName("employee_id")
            .IsRequired();

        builder.Property(x => x.Date)
            .HasColumnName("date")
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("assignment_type")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasColumnName("comment")
            .HasMaxLength(EmployeeWorkAssignment.CommentMaxLength);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsRowVersion();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.EmployeeId);

        builder.HasIndex(x => x.Date);

        builder.HasIndex(x => new
        {
            x.EmployeeId,
            x.Date
        });

        builder.HasIndex(x => new
            {
                x.EmployeeId,
                x.Date
            })
            .HasDatabaseName("IX_employee_work_assignments_employee_id_date_active")
            .IsUnique()
            .HasFilter("\"status\" = 'Active'");
    }
}
