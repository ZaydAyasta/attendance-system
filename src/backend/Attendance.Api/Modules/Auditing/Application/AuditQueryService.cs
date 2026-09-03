using System.Text.Json;
using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Auditing.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Api.Modules.Auditing.Application;

public sealed class AuditQueryService(AttendanceDbContext dbContext)
{
    public async Task<PagedAuditEventsResponse> ListAsync(DateOnly? from, DateOnly? to, Guid? actorUserId, string? action, string? entityType, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.AuditEvents.AsNoTracking().AsQueryable();
        if (from is not null) query = query.Where(x => x.OccurredAt >= new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)));
        if (to is not null) query = query.Where(x => x.OccurredAt < new DateTimeOffset(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)));
        if (actorUserId is not null) query = query.Where(x => x.ActorUserId == actorUserId);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, x.ActorUserId, x.ActorDisplayName, x.Action, x.EntityType, x.EntityId, x.OccurredAt, x.Outcome, x.Metadata })
            .ToListAsync(cancellationToken);
        return new PagedAuditEventsResponse(items.Select(x => new AuditEventResponse(x.Id, new AuditActorResponse(x.ActorUserId, x.ActorDisplayName), x.Action, x.EntityType, x.EntityId, x.OccurredAt, x.Outcome, ParseMetadata(x.Metadata))).ToList(), page, pageSize, total);
    }

    private static JsonElement? ParseMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata)) return null;
        using var document = JsonDocument.Parse(metadata);
        return document.RootElement.Clone();
    }
}
