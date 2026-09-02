using Attendance.Api.Modules.LegacyMigration.Domain;
using Xunit;

namespace Attendance.Api.Tests.LegacyMigration;

public sealed class LegacyImportMappingTests
{
    [Fact]
    public void Create_WithStableLegacyIdentity_PreservesAllIdempotencyFields()
    {
        var destinationId = Guid.NewGuid();
        var importedAt = new DateTimeOffset(2026, 9, 2, 15, 0, 0, TimeSpan.Zero);

        var mapping = LegacyImportMapping.Create(
            "legacy",
            "employee",
            "legacy-employee-42",
            destinationId,
            importedAt);

        Assert.NotEqual(Guid.Empty, mapping.Id);
        Assert.Equal("legacy", mapping.SourceSystem);
        Assert.Equal("employee", mapping.EntityType);
        Assert.Equal("legacy-employee-42", mapping.LegacyId);
        Assert.Equal(destinationId, mapping.DestinationId);
        Assert.Equal(importedAt, mapping.ImportedAt);
    }

    [Fact]
    public void Create_WithMissingLegacyId_RejectsAnUntrackableRecord()
    {
        var exception = Assert.Throws<ArgumentException>(() => LegacyImportMapping.Create(
            "legacy",
            "attendance-mark",
            " ",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));

        Assert.Equal("legacyId", exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyDestinationId_RejectsAnOrphanMapping()
    {
        var exception = Assert.Throws<ArgumentException>(() => LegacyImportMapping.Create(
            "legacy",
            "employee",
            "legacy-employee-42",
            Guid.Empty,
            DateTimeOffset.UtcNow));

        Assert.Equal("destinationId", exception.ParamName);
    }
}
