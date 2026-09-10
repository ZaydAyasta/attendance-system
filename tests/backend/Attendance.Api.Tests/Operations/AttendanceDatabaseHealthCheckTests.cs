using Attendance.Api.BuildingBlocks.Operations;
using Attendance.Api.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Attendance.Api.Tests.Operations;

public sealed class AttendanceDatabaseHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseCanConnect_ReturnsHealthy()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AttendanceDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        await using var provider = services.BuildServiceProvider();
        var healthCheck = new AttendanceDatabaseHealthCheck(provider.GetRequiredService<IServiceScopeFactory>());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Database is available.", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseCannotBeResolved_ReturnsSafeUnhealthyResponse()
    {
        var services = new ServiceCollection();
        await using var provider = services.BuildServiceProvider();
        var healthCheck = new AttendanceDatabaseHealthCheck(provider.GetRequiredService<IServiceScopeFactory>());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Database is unavailable.", result.Description);
        Assert.Null(result.Exception);
    }
}
