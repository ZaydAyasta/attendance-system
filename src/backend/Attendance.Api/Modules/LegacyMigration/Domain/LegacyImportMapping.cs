namespace Attendance.Api.Modules.LegacyMigration.Domain;

/// <summary>
/// Records the destination identity assigned to one immutable legacy record.
/// It is intentionally separate from operational entities so a rerun can be
/// decided without relying on names or timestamps.
/// </summary>
public sealed class LegacyImportMapping
{
    public Guid Id { get; private set; }

    public string SourceSystem { get; private set; } = null!;

    public string EntityType { get; private set; } = null!;

    public string LegacyId { get; private set; } = null!;

    public Guid DestinationId { get; private set; }

    public DateTimeOffset ImportedAt { get; private set; }

    private LegacyImportMapping()
    {
    }

    private LegacyImportMapping(
        Guid id,
        string sourceSystem,
        string entityType,
        string legacyId,
        Guid destinationId,
        DateTimeOffset importedAt)
    {
        Id = id;
        SourceSystem = Required(sourceSystem, nameof(sourceSystem));
        EntityType = Required(entityType, nameof(entityType));
        LegacyId = Required(legacyId, nameof(legacyId));
        DestinationId = destinationId == Guid.Empty
            ? throw new ArgumentException("DestinationId must be non-empty.", nameof(destinationId))
            : destinationId;
        ImportedAt = importedAt == default
            ? throw new ArgumentException("ImportedAt must be non-default.", nameof(importedAt))
            : importedAt;
    }

    public static LegacyImportMapping Create(
        string sourceSystem,
        string entityType,
        string legacyId,
        Guid destinationId,
        DateTimeOffset importedAt)
        => new(Guid.NewGuid(), sourceSystem, entityType, legacyId, destinationId, importedAt);

    private static string Required(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("A non-empty value is required.", parameterName)
            : value.Trim();
}
