using Attendance.Api.Modules.Attendance.Domain;
using Xunit;

namespace Attendance.Api.Tests.Attendance;

public sealed class AttendanceMarkTests
{
    [Fact]
    public void Create_WithValidData_ReturnsMark()
    {
        var employeeId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 8, 20, 13, 15, 0, TimeSpan.Zero);
        var checkpointId = Guid.NewGuid();

        var mark = AttendanceMark.Create(
            employeeId,
            occurredAt,
            AttendanceMarkType.Entry,
            AttendanceSource.Manual,
            checkpointId);

        Assert.NotEqual(Guid.Empty, mark.Id);
        Assert.Equal(employeeId, mark.EmployeeId);
        Assert.Equal(occurredAt, mark.OccurredAt);
        Assert.Equal(AttendanceMarkType.Entry, mark.Type);
        Assert.Equal(AttendanceSource.Manual, mark.Source);
        Assert.Equal(checkpointId, mark.CheckpointId);
    }

    [Fact]
    public void Create_WithEmptyEmployeeId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            AttendanceMark.Create(
                Guid.Empty,
                new DateTimeOffset(2026, 8, 20, 13, 15, 0, TimeSpan.Zero),
                AttendanceMarkType.Entry,
                AttendanceSource.Manual,
                checkpointId: null));

        Assert.Equal("employeeId", exception.ParamName);
    }

    [Fact]
    public void Create_WithDefaultOccurredAt_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            AttendanceMark.Create(
                Guid.NewGuid(),
                default,
                AttendanceMarkType.Entry,
                AttendanceSource.Manual,
                checkpointId: null));

        Assert.Equal("occurredAt", exception.ParamName);
    }

    [Fact]
    public void Create_WithInvalidType_ThrowsArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            AttendanceMark.Create(
                Guid.NewGuid(),
                new DateTimeOffset(2026, 8, 20, 13, 15, 0, TimeSpan.Zero),
                (AttendanceMarkType)999,
                AttendanceSource.Manual,
                checkpointId: null));

        Assert.Equal("type", exception.ParamName);
    }

    [Fact]
    public void Create_WithInvalidSource_ThrowsArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            AttendanceMark.Create(
                Guid.NewGuid(),
                new DateTimeOffset(2026, 8, 20, 13, 15, 0, TimeSpan.Zero),
                AttendanceMarkType.Entry,
                (AttendanceSource)999,
                checkpointId: null));

        Assert.Equal("source", exception.ParamName);
    }

    [Fact]
    public void Create_WithNullCheckpointId_AllowsNull()
    {
        var mark = AttendanceMark.Create(
            Guid.NewGuid(),
            new DateTimeOffset(2026, 8, 20, 13, 15, 0, TimeSpan.Zero),
            AttendanceMarkType.Entry,
            AttendanceSource.Manual,
            checkpointId: null);

        Assert.Null(mark.CheckpointId);
    }

    [Fact]
    public void Create_WithCheckpointId_AllowsValue()
    {
        var checkpointId = Guid.NewGuid();

        var mark = AttendanceMark.Create(
            Guid.NewGuid(),
            new DateTimeOffset(2026, 8, 20, 13, 15, 0, TimeSpan.Zero),
            AttendanceMarkType.Entry,
            AttendanceSource.Manual,
            checkpointId);

        Assert.Equal(checkpointId, mark.CheckpointId);
    }
}
