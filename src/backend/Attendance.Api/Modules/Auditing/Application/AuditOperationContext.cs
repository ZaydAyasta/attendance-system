using System.Security.Claims;
using System.Text.Json;
using Attendance.Api.Modules.Auditing.Domain;

namespace Attendance.Api.Modules.Auditing.Application;

public sealed class AuditOperationContext
{
    private PendingAuditOperation? operation;

    public PendingAuditOperation? Consume()
    {
        var pending = operation;
        operation = null;
        return pending;
    }

    public void Set(ClaimsPrincipal actor, string action, string entityType, string? entityId, object? metadata = null)
    {
        var id = actor.FindFirstValue(ClaimTypes.NameIdentifier);
        operation = new PendingAuditOperation(
            Guid.TryParse(id, out var actorUserId) ? actorUserId : null,
            actor.Identity?.Name ?? "Sistema",
            action,
            entityType,
            entityId,
            metadata is null ? null : JsonSerializer.Serialize(metadata));
    }

    public void UpdateMetadata(object metadata)
    {
        if (operation is not null) operation = operation with { Metadata = JsonSerializer.Serialize(metadata) };
    }

    public sealed record PendingAuditOperation(Guid? ActorUserId, string ActorDisplayName, string Action, string EntityType, string? EntityId, string? Metadata);
}
