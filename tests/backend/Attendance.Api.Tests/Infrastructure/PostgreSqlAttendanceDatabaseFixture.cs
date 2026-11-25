using Attendance.Api.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Attendance.Api.Tests.Infrastructure;

public sealed class PostgreSqlAttendanceDatabaseFixture : IAsyncLifetime
{
    public const string LocalServerVariable = "ATTENDANCE_TEST_POSTGRES";
    private PostgreSqlContainer? container;
    private string? localConnectionString;

    public string? SkipReason { get; private set; }

    public bool IsAvailable => SkipReason is null;

    public string ConnectionString => localConnectionString ?? container?.GetConnectionString()
        ?? throw new InvalidOperationException("PostgreSQL container is not available.");

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable(LocalServerVariable) is { Length: > 0 } configuredServer)
        {
            var localServer = new NpgsqlConnectionStringBuilder(configuredServer);
            if (localServer.Host is not ("127.0.0.1" or "localhost" or "::1"))
                throw new InvalidOperationException($"{LocalServerVariable} requires a loopback PostgreSQL server.");

            localServer.Database = $"attendance_tests_{Guid.NewGuid():N}";
            localConnectionString = localServer.ConnectionString;
            await using var database = CreateDbContext();
            await database.Database.MigrateAsync();
            return;
        }

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
        if (localConnectionString is not null)
        {
            await using var database = CreateDbContext();
            await database.Database.EnsureDeletedAsync();
        }

        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }

    public AttendanceDbContext CreateDbContext()
    {
        ThrowIfUnavailable();

        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseNpgsql(ConnectionString)
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
