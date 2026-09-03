using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Auditing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Attendance.Api.Modules.Auditing.Application;

public sealed class AuditSaveChangesInterceptor(AuditOperationContext operationContext) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AddPendingEvent(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddPendingEvent(DbContext? context)
    {
        if (context is not AttendanceDbContext attendanceContext || attendanceContext.ChangeTracker.Entries<AuditEvent>().Any()) return;
        var pending = operationContext.Consume();
        if (pending is null) return;
        var entityId = pending.EntityId ?? ResolveEntityId(attendanceContext, pending.EntityType);
        if (entityId is not null)
            attendanceContext.AuditEvents.Add(AuditEvent.Succeeded(pending.ActorUserId, pending.ActorDisplayName, pending.Action, pending.EntityType, entityId, pending.Metadata));
    }

    private static string? ResolveEntityId(AttendanceDbContext context, string entityType)
    {
        var entry = context.ChangeTracker.Entries()
            .FirstOrDefault(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted && x.Metadata.ClrType.Name == entityType);
        if (entry is null) return null;
        var property = entry.Properties.FirstOrDefault(x => x.Metadata.Name is "Id" or "Date");
        return property?.CurrentValue?.ToString() ?? property?.OriginalValue?.ToString();
    }
}
