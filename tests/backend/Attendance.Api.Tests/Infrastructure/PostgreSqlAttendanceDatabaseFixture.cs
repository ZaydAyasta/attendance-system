using Attendance.Api.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Attendance.Api.Tests.Infrastructure;

public sealed class PostgreSqlAttendanceDatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? container;

    public string? SkipReason { get; private set; }

    public bool IsAvailable => SkipReason is null;

    public async Task InitializeAsync()
    {
        if (!HasContainerRuntime())
        {
            SkipReason =
                "PostgreSQL integration tests require a Docker-compatible runtime.";
            return;
        }

        try
        {
            container = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("attendance_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await container.StartAsync();

            await using var dbContext = CreateDbContext();
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception exception)
        {
            SkipReason =
                $"PostgreSQL integration tests could not start: {exception.Message}";

            if (container is not null)
            {
                await container.DisposeAsync();
                container = null;
            }
        }
    }

    public async Task DisposeAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }

    public AttendanceDbContext CreateDbContext()
    {
        ThrowIfUnavailable();

        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseNpgsql(container!.GetConnectionString())
            .Options;

        return new AttendanceDbContext(options);
    }

    public async Task ResetAsync()
    {
        ThrowIfUnavailable();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    public void ThrowIfUnavailable()
    {
        if (SkipReason is not null)
        {
            throw new InvalidOperationException(SkipReason);
        }
    }

    private static bool HasContainerRuntime()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST")))
        {
            return true;
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return File.Exists("/var/run/docker.sock")
               || File.Exists(Path.Combine(userProfile, ".docker", "run", "docker.sock"));
    }
}
