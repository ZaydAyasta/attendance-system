using Attendance.LegacyImporter;
using Xunit;

namespace Attendance.Api.Tests.LegacyMigration;

[Collection("Legacy connection environment")]
public sealed class LegacyConnectionSettingsTests
{
    [Fact]
    public void FromEnvironment_WithPostgreSqlUrl_NormalizesTheSourceAndKeepsTheDestinationIdentity()
    {
        const string source = "postgresql://legacy_migration_reader:complex%40password@legacy.example.test:6543/legacy_data";
        const string destination = "Host=127.0.0.1;Port=5432;Database=attendance_dev;Username=attendance_app;Password=local";
        var previousSource = Environment.GetEnvironmentVariable("LEGACY_DATABASE_URL");
        var previousDestination = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        try
        {
            Environment.SetEnvironmentVariable("LEGACY_DATABASE_URL", source);
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", destination);

            var settings = LegacyConnectionSettings.FromEnvironment();

            Assert.Equal("legacy.example.test", settings.Source.Host);
            Assert.Equal(6543, settings.Source.Port);
            Assert.Equal("legacy_data", settings.Source.Database);
            Assert.Equal("legacy_migration_reader", settings.Source.Username);
            Assert.Equal("127.0.0.1", settings.Destination.Host);
            Assert.Equal("attendance_dev", settings.Destination.Database);
            Assert.Contains("SSL Mode=Require", settings.SourceConnectionString, StringComparison.Ordinal);
            Assert.Contains("GSS Encryption Mode=Disable", settings.SourceConnectionString, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("LEGACY_DATABASE_URL", previousSource);
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", previousDestination);
        }
    }
}

[CollectionDefinition("Legacy connection environment", DisableParallelization = true)]
public sealed class LegacyConnectionEnvironmentCollection;
