using Attendance.Api.Modules.Checkpoints.Domain;
using Xunit;

namespace Attendance.Api.Tests.Checkpoints;

public sealed class CheckpointTests
{
    [Fact]
    public void Create_GeneralCheckpoint_RequiresStaticQr()
    {
        var exception = Assert.Throws<ArgumentException>(() => Checkpoint.Create(
            "GENERAL-01",
            "Checkpoint general",
            CheckpointType.General,
            CheckpointQrMode.Dynamic));

        Assert.Equal("qrMode", exception.ParamName);
    }

    [Fact]
    public void Create_GeneralCheckpoint_WithStaticQr_PreservesItsConfiguration()
    {
        var checkpoint = Checkpoint.Create(
            "GENERAL-01",
            "Checkpoint general",
            CheckpointType.General,
            CheckpointQrMode.Static);

        Assert.Equal(CheckpointType.General, checkpoint.Type);
        Assert.Equal(CheckpointQrMode.Static, checkpoint.QrMode);
    }
}
