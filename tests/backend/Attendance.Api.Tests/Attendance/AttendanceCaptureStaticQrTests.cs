using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Attendance.Application;
using Attendance.Api.Modules.Checkpoints.Application;
using Attendance.Api.Modules.Checkpoints.Domain;
using Attendance.Api.Modules.Employees.Domain;
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
        var qrService = new CheckpointQrService(
            DataProtectionProvider.Create(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))),
            new ConfigurationBuilder().AddInMemoryCollection().Build());
        var employee = Employee.Create("EMP-1", "Ana", "Torres", new DateOnly(2025, 1, 1));
        checkpoint.SetStaticQrToken(qrService.CreateStaticToken());
        dbContext.Checkpoints.Add(checkpoint);
        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync();
        var service = new AttendanceCaptureService(dbContext, new AttendanceTimeZone("UTC"), qrService);

        var result = await service.ResolveAsync(employee.Id, checkpoint.StaticQrToken!, CancellationToken.None);

        Assert.Equal(AttendanceCaptureStatus.Success, result.Status);
        Assert.Equal(checkpoint.Id, result.Value!.Checkpoint.Id);
        Assert.Equal(["Entry"], result.Value.AvailableActions);
        var unknownToken = await service.ResolveAsync(employee.Id, qrService.CreateStaticToken(), CancellationToken.None);
        Assert.Equal(AttendanceCaptureStatus.InvalidToken, unknownToken.Status);

        employee.SetActive(false, new DateOnly(2025, 6, 30));
        await dbContext.SaveChangesAsync();
        var inactive = await service.MarkAsync(employee.Id, checkpoint.StaticQrToken!, "Entry", CancellationToken.None);
        Assert.Equal(AttendanceCaptureStatus.EmployeeUnavailable, inactive.Status);
        Assert.Empty(dbContext.AttendanceMarks);
    }
}
