using Attendance.LegacyImporter;
using Xunit;

namespace Attendance.Api.Tests.LegacyMigration;

public sealed class LegacySourceSafetyGuardTests
{
    [Fact]
    public void ValidateEndpoints_WhenSourceAndDestinationAreTheSame_RejectsImport()
    {
        var source = new DatabaseIdentity("localhost", 5432, "attendance_dev", "legacy_migration_reader");
        var destination = new DatabaseIdentity("localhost", 5432, "attendance_dev", "attendance_app");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LegacySourceSafetyGuard.ValidateEndpoints(source, destination));

        Assert.Contains("same database", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateEndpoints_WhenDestinationIsNotLocalAttendanceDev_RejectsImport()
    {
        var source = new DatabaseIdentity("legacy.example.test", 5432, "legacy", "legacy_migration_reader");
        var destination = new DatabaseIdentity("legacy.example.test", 5432, "attendance_prod", "attendance_app");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LegacySourceSafetyGuard.ValidateEndpoints(source, destination));

        Assert.Contains("local attendance_dev", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateEndpoints_WhenSourceIsRemoteAndDestinationIsLocalAttendanceDev_AllowsDiscovery()
    {
        var source = new DatabaseIdentity("legacy.example.test", 5432, "legacy", "legacy_migration_reader");
        var destination = new DatabaseIdentity("127.0.0.1", 5432, "attendance_dev", "attendance_app");

        LegacySourceSafetyGuard.ValidateEndpoints(source, destination);
    }
}
