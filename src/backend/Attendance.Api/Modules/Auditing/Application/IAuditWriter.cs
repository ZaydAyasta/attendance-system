using System.Security.Claims;

namespace Attendance.Api.Modules.Auditing.Application;

public interface IAuditWriter
{
    void Prepare(ClaimsPrincipal actor, string action, string entityType, string? entityId = null, object? metadata = null);
}
