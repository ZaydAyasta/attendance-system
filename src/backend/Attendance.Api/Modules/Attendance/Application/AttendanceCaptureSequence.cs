using Attendance.Api.Modules.Attendance.Domain;
using Attendance.Api.Modules.Checkpoints.Domain;

namespace Attendance.Api.Modules.Attendance.Application;

public static class AttendanceCaptureSequence
{
    public static IReadOnlyCollection<AttendanceMarkType> Resolve(IReadOnlyCollection<AttendanceMarkType> types, CheckpointType checkpointType)
    {
        if (types.Contains(AttendanceMarkType.Exit)) return Array.Empty<AttendanceMarkType>();
        if (!types.Contains(AttendanceMarkType.Entry)) return checkpointType == CheckpointType.EntryExit ? [AttendanceMarkType.Entry] : Array.Empty<AttendanceMarkType>();
        var last = types.LastOrDefault();
        if (last == AttendanceMarkType.LunchStart) return [AttendanceMarkType.LunchEnd];
        if (last == AttendanceMarkType.CommissionExit) return checkpointType == CheckpointType.EntryExit ? [AttendanceMarkType.CommissionReturn] : Array.Empty<AttendanceMarkType>();
        if (last == AttendanceMarkType.OtherExit) return checkpointType == CheckpointType.EntryExit ? [AttendanceMarkType.OtherReturn] : Array.Empty<AttendanceMarkType>();
        return checkpointType == CheckpointType.Cafeteria
            ? [AttendanceMarkType.LunchStart]
            : [AttendanceMarkType.LunchStart, AttendanceMarkType.CommissionExit, AttendanceMarkType.OtherExit, AttendanceMarkType.Exit];
    }
}
