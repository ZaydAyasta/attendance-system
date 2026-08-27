using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Attendance.Api.Modules.Checkpoints.Application;

public sealed record CheckpointQrPayload(Guid CheckpointId, string Nonce, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt);
public enum CheckpointQrValidationStatus { Valid, Invalid, Expired, Replayed }
public sealed record CheckpointQrValidationResult(CheckpointQrValidationStatus Status, CheckpointQrPayload? Payload = null);

public sealed class CheckpointQrService(IDataProtectionProvider dataProtectionProvider, IConfiguration configuration)
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("Attendance.CheckpointQr.v1");
    private readonly ConcurrentDictionary<string, DateTimeOffset> _reservations = new();
    private readonly TimeSpan _lifetime = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue<int?>("CheckpointQr:TokenLifetimeSeconds") ?? 30, 10, 300));

    public CheckpointQrPayload CreatePayload(Guid checkpointId)
    {
        var issuedAt = DateTimeOffset.UtcNow;
        return new(checkpointId, Convert.ToHexString(RandomNumberGenerator.GetBytes(16)), issuedAt, issuedAt.Add(_lifetime));
    }

    public string Protect(CheckpointQrPayload payload) => _protector.Protect(JsonSerializer.Serialize(payload));

    public CheckpointQrValidationResult Validate(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return new(CheckpointQrValidationStatus.Invalid);
        try
        {
            var payload = JsonSerializer.Deserialize<CheckpointQrPayload>(_protector.Unprotect(token));
            if (payload is null || payload.CheckpointId == Guid.Empty || string.IsNullOrWhiteSpace(payload.Nonce) || payload.IssuedAt > payload.ExpiresAt)
                return new(CheckpointQrValidationStatus.Invalid);
            return payload.ExpiresAt <= DateTimeOffset.UtcNow
                ? new(CheckpointQrValidationStatus.Expired)
                : new(CheckpointQrValidationStatus.Valid, payload);
        }
        catch (CryptographicException) { return new(CheckpointQrValidationStatus.Invalid); }
        catch (JsonException) { return new(CheckpointQrValidationStatus.Invalid); }
    }

    public bool TryReserve(CheckpointQrPayload payload, Guid employeeId)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var item in _reservations.Where(x => x.Value <= now).ToArray()) _reservations.TryRemove(item.Key, out _);
        return _reservations.TryAdd($"{payload.Nonce}:{employeeId:N}", payload.ExpiresAt);
    }

    public void Release(CheckpointQrPayload payload, Guid employeeId) => _reservations.TryRemove($"{payload.Nonce}:{employeeId:N}", out _);
}
