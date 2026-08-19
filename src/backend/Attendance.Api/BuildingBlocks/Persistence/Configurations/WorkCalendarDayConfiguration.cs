using Attendance.Api.Modules.WorkCalendar.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Api.BuildingBlocks.Persistence.Configurations;

/// <summary>
/// Defines the database mapping for <see cref="WorkCalendarDay"/>.
/// </summary>
public sealed class WorkCalendarDayConfiguration
    : IEntityTypeConfiguration<WorkCalendarDay>
{
    public void Configure(EntityTypeBuilder<WorkCalendarDay> builder)
    {
        builder.ToTable("work_calendar_days");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Date)
            .HasColumnName("date")
            .IsRequired();

        builder.HasIndex(x => x.Date)
            .IsUnique();

        builder.Property(x => x.DayType)
            .HasColumnName("day_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsRowVersion();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(500);
    }
}