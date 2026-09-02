using Attendance.Api.Modules.LegacyMigration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Api.BuildingBlocks.Persistence.Configurations;

public sealed class LegacyImportMappingConfiguration : IEntityTypeConfiguration<LegacyImportMapping>
{
    public void Configure(EntityTypeBuilder<LegacyImportMapping> builder)
    {
        builder.ToTable("legacy_import_mappings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SourceSystem).HasColumnName("source_system").HasMaxLength(50).IsRequired();
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(50).IsRequired();
        builder.Property(x => x.LegacyId).HasColumnName("legacy_id").HasMaxLength(120).IsRequired();
        builder.Property(x => x.DestinationId).HasColumnName("destination_id").IsRequired();
        builder.Property(x => x.ImportedAt).HasColumnName("imported_at").IsRequired();

        builder.HasIndex(x => new { x.SourceSystem, x.EntityType, x.LegacyId }).IsUnique();
    }
}
