using System.Security.Claims;

namespace Attendance.Api.Modules.Auditing.Application;

/// <summary>Queues one successful administrative operation for atomic persistence by EF.</summary>
public sealed class AuditWriter(AuditOperationContext operationContext) : IAuditWriter
{
    public void Prepare(ClaimsPrincipal actor, string action, string entityType, string? entityId = null, object? metadata = null)
        => operationContext.Set(actor, action, entityType, entityId, metadata);
}
