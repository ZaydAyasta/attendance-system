using System.Text.Json;

namespace Attendance.Api.Modules.Auditing.Contracts;

public sealed record AuditActorResponse(Guid? UserId, string DisplayName);

public sealed record AuditEventResponse(
    Guid Id,
    AuditActorResponse Actor,
    string Action,
    string EntityType,
    string EntityId,
    DateTimeOffset OccurredAt,
    string Outcome,
    JsonElement? Metadata);

public sealed record PagedAuditEventsResponse(IReadOnlyList<AuditEventResponse> Items, int Page, int PageSize, int Total);
