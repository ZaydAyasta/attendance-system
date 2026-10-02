namespace Attendance.Api.Modules.Checkpoints.Domain;

public sealed class Checkpoint
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public CheckpointType Type { get; private set; }
    public CheckpointQrMode QrMode { get; private set; }
    public string? StaticQrToken { get; private set; }
    public bool IsActive { get; private set; }
    public uint Version { get; private set; }

    private Checkpoint() { }

    private Checkpoint(Guid id, string code, string name, CheckpointType type, CheckpointQrMode qrMode)
    {
        Id = id;
        Update(code, name, type, qrMode);
        IsActive = true;
    }

    public static Checkpoint Create(string code, string name, CheckpointType type, CheckpointQrMode qrMode)
        => new(Guid.NewGuid(), code, name, type, qrMode);

    public void Update(string code, string name, CheckpointType type, CheckpointQrMode qrMode)
    {
        Code = RequireText(code, nameof(code), 60).ToUpperInvariant();
        Name = RequireText(name, nameof(name), 160);
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        if (!Enum.IsDefined(qrMode)) throw new ArgumentOutOfRangeException(nameof(qrMode));
        if (type == CheckpointType.General && qrMode != CheckpointQrMode.Static)
            throw new ArgumentException("General checkpoints require a static QR mode.", nameof(qrMode));
        Type = type;
        QrMode = qrMode;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    public void SetQrMode(CheckpointQrMode qrMode)
    {
        if (!Enum.IsDefined(qrMode)) throw new ArgumentOutOfRangeException(nameof(qrMode));
        if (Type == CheckpointType.General && qrMode != CheckpointQrMode.Static)
            throw new ArgumentException("General checkpoints require a static QR mode.", nameof(qrMode));
        QrMode = qrMode;
    }

    public void SetStaticQrToken(string token)
    {
        if (QrMode != CheckpointQrMode.Static) throw new InvalidOperationException("Only static checkpoints can have a static QR token.");
        StaticQrToken = RequireText(token, nameof(token), 2048);
    }

    private static string RequireText(string value, string parameterName, int maximumLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > maximumLength)
            throw new ArgumentException($"{parameterName} is required and must not exceed {maximumLength} characters.", parameterName);
        return normalized;
    }
}
