namespace Attendance.Api.Modules.Auditing.Domain;

public sealed class AuditEvent
{
    private AuditEvent() { }

    private AuditEvent(Guid? actorUserId, string actorDisplayName, string action, string entityType, string entityId, string? metadata)
    {
        Id = Guid.NewGuid();
        ActorUserId = actorUserId;
        ActorDisplayName = actorDisplayName;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        OccurredAt = DateTimeOffset.UtcNow;
        Outcome = "Succeeded";
        Metadata = metadata;
    }

    public Guid Id { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string ActorDisplayName { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public string Outcome { get; private set; } = string.Empty;
    public string? Metadata { get; private set; }

    public static AuditEvent Succeeded(Guid? actorUserId, string actorDisplayName, string action, string entityType, string entityId, string? metadata)
        => new(actorUserId, actorDisplayName, action, entityType, entityId, metadata);
}
