using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Attendance.Application;
using Attendance.Api.Modules.Attendance.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Attendance.Api.Tests.Attendance;

public sealed class DailyAttendanceServiceBasicTests
{
    private static readonly AttendanceTimeZone AttendanceTimeZone =
        new("America/Lima");

    [Fact]
    public async Task GetByDateAsync_ReturnsEmployeeNotFoundWhenEmployeeDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.GetByDateAsync(
            Guid.NewGuid(),
            new DailyAttendanceQuery(new DateOnly(2026, 8, 20)),
            CancellationToken.None);

        Assert.Equal(AttendanceQueryStatus.EmployeeNotFound, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetRangeAsync_ReturnsEmployeeNotFoundWhenEmployeeDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.GetRangeAsync(
            Guid.NewGuid(),
            new AttendanceRangeQuery(
                new DateOnly(2026, 8, 20),
                new DateOnly(2026, 8, 22)),
            CancellationToken.None);

        Assert.Equal(AttendanceQueryStatus.EmployeeNotFound, result.Status);
        Assert.Null(result.Value);
    }

    private static AttendanceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AttendanceDbContext(options);
    }

    private static DailyAttendanceService CreateService(
        AttendanceDbContext dbContext)
        => new(
            dbContext,
            new AttendanceEvaluator(),
            new AttendanceTimeCalculator(),
            AttendanceTimeZone);
}
