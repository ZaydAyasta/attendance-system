using Attendance.Api.Modules.Checkpoints.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Api.BuildingBlocks.Persistence.Configurations;

public sealed class CheckpointConfiguration : IEntityTypeConfiguration<Checkpoint>
{
    public void Configure(EntityTypeBuilder<Checkpoint> builder)
    {
        builder.ToTable("checkpoints");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(60).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Type).HasColumnName("checkpoint_type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
