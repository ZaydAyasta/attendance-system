using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Checkpoints.Contracts;
using Attendance.Api.Modules.Checkpoints.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Attendance.Api.Modules.Checkpoints.Application;

public enum CheckpointWriteStatus { Success, NotFound, Duplicate, ConcurrencyConflict, Invalid }
public sealed record CheckpointWriteResult(CheckpointWriteStatus Status, CheckpointResponse? Value = null);

public sealed class CheckpointService(AttendanceDbContext dbContext)
{
    public Task<List<CheckpointResponse>> ListAsync(CancellationToken cancellationToken) => dbContext.Checkpoints.AsNoTracking()
        .OrderBy(x => x.Name).Select(x => new CheckpointResponse(x.Id, x.Code, x.Name, x.Type.ToString(), x.IsActive, x.Version)).ToListAsync(cancellationToken);

    public async Task<CheckpointWriteResult> CreateAsync(CreateCheckpointRequest request, CancellationToken cancellationToken)
    {
        if (!TryType(request.Type, out var type)) return new(CheckpointWriteStatus.Invalid);
        var checkpoint = Checkpoint.Create(request.Code, request.Name, type);
        dbContext.Checkpoints.Add(checkpoint);
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { return new(CheckpointWriteStatus.Duplicate); }
        return new(CheckpointWriteStatus.Success, Map(checkpoint));
    }

    public async Task<CheckpointWriteResult> UpdateAsync(Guid id, UpdateCheckpointRequest request, CancellationToken cancellationToken)
    {
        if (!TryType(request.Type, out var type)) return new(CheckpointWriteStatus.Invalid);
        var checkpoint = await dbContext.Checkpoints.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (checkpoint is null) return new(CheckpointWriteStatus.NotFound);
        checkpoint.Update(request.Code, request.Name, type);
        dbContext.Entry(checkpoint).Property(x => x.Version).OriginalValue = request.Version;
        return await SaveAsync(checkpoint, cancellationToken);
    }

    public async Task<CheckpointWriteResult> SetStatusAsync(Guid id, SetCheckpointStatusRequest request, CancellationToken cancellationToken)
    {
        var checkpoint = await dbContext.Checkpoints.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (checkpoint is null) return new(CheckpointWriteStatus.NotFound);
        checkpoint.SetActive(request.IsActive);
        dbContext.Entry(checkpoint).Property(x => x.Version).OriginalValue = request.Version;
        return await SaveAsync(checkpoint, cancellationToken);
    }

    public Task<Checkpoint?> FindAsync(Guid id, CancellationToken cancellationToken) => dbContext.Checkpoints.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    private async Task<CheckpointWriteResult> SaveAsync(Checkpoint checkpoint, CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return new(CheckpointWriteStatus.ConcurrencyConflict); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { return new(CheckpointWriteStatus.Duplicate); }
        return new(CheckpointWriteStatus.Success, Map(checkpoint));
    }

    private static bool TryType(string value, out CheckpointType type) => Enum.TryParse(value, true, out type) && Enum.IsDefined(type);
    public static CheckpointResponse Map(Checkpoint checkpoint) => new(checkpoint.Id, checkpoint.Code, checkpoint.Name, checkpoint.Type.ToString(), checkpoint.IsActive, checkpoint.Version);
}
