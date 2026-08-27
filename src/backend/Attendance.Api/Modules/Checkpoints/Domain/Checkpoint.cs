namespace Attendance.Api.Modules.Checkpoints.Domain;

public sealed class Checkpoint
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public CheckpointType Type { get; private set; }
    public bool IsActive { get; private set; }
    public uint Version { get; private set; }

    private Checkpoint() { }

    private Checkpoint(Guid id, string code, string name, CheckpointType type)
    {
        Id = id;
        Update(code, name, type);
        IsActive = true;
    }

    public static Checkpoint Create(string code, string name, CheckpointType type)
        => new(Guid.NewGuid(), code, name, type);

    public void Update(string code, string name, CheckpointType type)
    {
        Code = RequireText(code, nameof(code), 60).ToUpperInvariant();
        Name = RequireText(name, nameof(name), 160);
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        Type = type;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    private static string RequireText(string value, string parameterName, int maximumLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > maximumLength)
            throw new ArgumentException($"{parameterName} is required and must not exceed {maximumLength} characters.", parameterName);
        return normalized;
    }
}
