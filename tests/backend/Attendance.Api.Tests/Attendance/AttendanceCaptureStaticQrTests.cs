using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Attendance.Application;
using Attendance.Api.Modules.Checkpoints.Application;
using Attendance.Api.Modules.Checkpoints.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Attendance.Api.Tests.Attendance;

public sealed class AttendanceCaptureStaticQrTests
{
    [Fact]
    public async Task ResolveAsync_AcceptsTheShortStaticToken()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var dbContext = new AttendanceDbContext(options);
        var checkpoint = Checkpoint.Create("ESTATIC-01", "Estático", CheckpointType.General, CheckpointQrMode.Static);
        dbContext.Checkpoints.Add(checkpoint);
        await dbContext.SaveChangesAsync();
        var qrService = new CheckpointQrService(
            DataProtectionProvider.Create(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))),
            new ConfigurationBuilder().AddInMemoryCollection().Build());
        var service = new AttendanceCaptureService(dbContext, new AttendanceTimeZone("UTC"), qrService);

        var result = await service.ResolveAsync(Guid.NewGuid(), qrService.CreateStaticToken(checkpoint.Id), CancellationToken.None);

        Assert.Equal(AttendanceCaptureStatus.Success, result.Status);
        Assert.Equal(checkpoint.Id, result.Value!.Checkpoint.Id);
        Assert.Equal(["Entry"], result.Value.AvailableActions);
    }
}
