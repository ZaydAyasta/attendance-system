namespace Attendance.Api.Modules.Checkpoints.Contracts;

public sealed record CheckpointResponse(Guid Id, string Code, string Name, string Type, bool IsActive, uint Version, string QrMode);
public sealed record CreateCheckpointRequest(string Code, string Name, string Type, string? QrMode = null);
public sealed record UpdateCheckpointRequest(string Code, string Name, string Type, uint Version, string? QrMode = null);
public sealed record SetCheckpointStatusRequest(bool IsActive, uint Version);
public sealed record CheckpointQrResponse(string Token, DateTimeOffset? ExpiresAt);
public sealed record CheckpointSummaryResponse(Guid Id, string Code, string Name, string Type);
