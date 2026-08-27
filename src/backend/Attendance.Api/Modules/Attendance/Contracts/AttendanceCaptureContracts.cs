using Attendance.Api.Modules.Checkpoints.Contracts;

namespace Attendance.Api.Modules.Attendance.Contracts;

public sealed record ResolveAttendanceCaptureRequest(string QrToken);
public sealed record CreateAttendanceCaptureMarkRequest(string QrToken, string Action);
public sealed record AttendanceCaptureResolutionResponse(CheckpointSummaryResponse Checkpoint, IReadOnlyCollection<string> AvailableActions, string Message);
public sealed record AttendanceCaptureMarkResponse(Guid Id, string Type, DateTimeOffset OccurredAt, CheckpointSummaryResponse Checkpoint, string Message);
public sealed record MyAttendanceMarkResponse(Guid Id, string Type, DateTimeOffset OccurredAt, CheckpointSummaryResponse? Checkpoint);
