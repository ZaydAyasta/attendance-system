using Xunit;

namespace Attendance.Api.Tests.Infrastructure;

public sealed class RequiresContainerRuntimeFactAttribute : FactAttribute
{
    private const string SkipMessage =
        "PostgreSQL integration tests require a Docker-compatible runtime.";

    public RequiresContainerRuntimeFactAttribute()
    {
        if (!HasContainerRuntime())
        {
            Skip = SkipMessage;
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
