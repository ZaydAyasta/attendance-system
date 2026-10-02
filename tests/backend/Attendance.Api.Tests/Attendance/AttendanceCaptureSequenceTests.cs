using Attendance.Api.Modules.Attendance.Application;
using Attendance.Api.Modules.Attendance.Domain;
using Attendance.Api.Modules.Checkpoints.Domain;
using Xunit;

namespace Attendance.Api.Tests.Attendance;

public sealed class AttendanceCaptureSequenceTests
{
    [Fact] public void EntryExit_WithoutMarks_OnlyAllowsEntry() => Assert.Equal([AttendanceMarkType.Entry], AttendanceCaptureSequence.Resolve([], CheckpointType.EntryExit));
    [Fact] public void Cafeteria_AfterEntry_OnlyAllowsLunchStart() => Assert.Equal([AttendanceMarkType.LunchStart], AttendanceCaptureSequence.Resolve([AttendanceMarkType.Entry], CheckpointType.Cafeteria));
    [Fact] public void LunchStart_OnlyAllowsLunchEnd() => Assert.Equal([AttendanceMarkType.LunchEnd], AttendanceCaptureSequence.Resolve([AttendanceMarkType.Entry, AttendanceMarkType.LunchStart], CheckpointType.EntryExit));
    [Fact] public void CommissionExit_OnlyAllowsReturn() => Assert.Equal([AttendanceMarkType.CommissionReturn], AttendanceCaptureSequence.Resolve([AttendanceMarkType.Entry, AttendanceMarkType.CommissionExit], CheckpointType.EntryExit));
    [Fact] public void OtherExit_OnlyAllowsReturn() => Assert.Equal([AttendanceMarkType.OtherReturn], AttendanceCaptureSequence.Resolve([AttendanceMarkType.Entry, AttendanceMarkType.OtherExit], CheckpointType.EntryExit));
    [Fact] public void Exit_PreventsAnotherAction() => Assert.Empty(AttendanceCaptureSequence.Resolve([AttendanceMarkType.Entry, AttendanceMarkType.Exit], CheckpointType.EntryExit));
    [Fact] public void General_WithoutMarks_OnlyAllowsEntry() => Assert.Equal([AttendanceMarkType.Entry], AttendanceCaptureSequence.Resolve([], CheckpointType.General));
    [Fact] public void General_AfterEntry_OffersLunchOvertimeAndExit() => Assert.Equal([AttendanceMarkType.LunchStart, AttendanceMarkType.OvertimeStart, AttendanceMarkType.Exit], AttendanceCaptureSequence.Resolve([AttendanceMarkType.Entry], CheckpointType.General));
    [Fact] public void General_OvertimeStart_OnlyAllowsOvertimeEnd() => Assert.Equal([AttendanceMarkType.OvertimeEnd], AttendanceCaptureSequence.Resolve([AttendanceMarkType.Entry, AttendanceMarkType.OvertimeStart], CheckpointType.General));
    [Fact] public void General_OvertimeEnd_OnlyAllowsExit() => Assert.Equal([AttendanceMarkType.Exit], AttendanceCaptureSequence.Resolve([AttendanceMarkType.Entry, AttendanceMarkType.OvertimeStart, AttendanceMarkType.OvertimeEnd], CheckpointType.General));
}
