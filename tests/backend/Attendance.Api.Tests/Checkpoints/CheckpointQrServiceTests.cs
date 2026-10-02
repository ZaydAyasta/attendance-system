using Attendance.Api.Modules.Checkpoints.Application;
using Attendance.Api.Modules.Checkpoints.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Attendance.Api.Tests.Checkpoints;

public sealed class CheckpointQrServiceTests
{
    private static CheckpointQrService Create() => new(DataProtectionProvider.Create(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))), new ConfigurationBuilder().AddInMemoryCollection().Build());

    [Fact]
    public void ProtectedToken_IsValidatedAndReservedOncePerEmployee()
    {
        var service = Create(); var payload = service.CreatePayload(Guid.NewGuid()); var result = service.Validate(service.Protect(payload));
        Assert.Equal(CheckpointQrValidationStatus.Valid, result.Status);
        Assert.True(service.TryReserve(result.Payload!, Guid.NewGuid()));
    }

    [Fact]
    public void TamperedToken_IsRejected()
    {
        var service = Create(); var token = service.Protect(service.CreatePayload(Guid.NewGuid()));
        Assert.Equal(CheckpointQrValidationStatus.Invalid, service.Validate(token + "x").Status);
    }

    [Fact]
    public void ExpiredPayload_IsRejected()
    {
        var service = Create(); var now = DateTimeOffset.UtcNow; var payload = new CheckpointQrPayload(Guid.NewGuid(), "nonce", now.AddMinutes(-2), now.AddMinutes(-1));
        Assert.Equal(CheckpointQrValidationStatus.Expired, service.Validate(service.Protect(payload)).Status);
    }

    [Fact]
    public void StaticPayload_IsValidWithoutExpiryAndCanBeReused()
    {
        var service = Create();
        var payload = service.CreateStaticPayload(Guid.NewGuid());
        var result = service.Validate(service.Protect(payload));

        Assert.Equal(CheckpointQrValidationStatus.Valid, result.Status);
        Assert.Equal(CheckpointQrMode.Static, result.Payload!.Mode);
        Assert.Null(result.Payload.ExpiresAt);

        var employeeId = Guid.NewGuid();
        Assert.True(service.TryReserve(result.Payload, employeeId));
        Assert.True(service.TryReserve(result.Payload, employeeId));
    }
}
