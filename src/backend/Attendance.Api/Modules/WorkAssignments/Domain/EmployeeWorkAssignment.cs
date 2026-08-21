namespace Attendance.Api.Modules.WorkAssignments.Domain;

/// <summary>
/// Represents an exceptional day on which one employee is expected to work.
/// </summary>
public sealed class EmployeeWorkAssignment
{
    public const int CommentMaxLength = 500;

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    public DateOnly Date { get; private set; }

    public WorkAssignmentType Type { get; private set; }

    public string? Comment { get; private set; }

    public WorkAssignmentStatus Status { get; private set; }

    /// <summary>
    /// Gets the PostgreSQL row version used for optimistic concurrency control.
    /// </summary>
    public uint Version { get; private set; }

    private EmployeeWorkAssignment()
    {
    }

    private EmployeeWorkAssignment(
        Guid id,
        Guid employeeId,
        DateOnly date,
        WorkAssignmentType type,
        string? comment)
    {
        Id = id;
        EmployeeId = EnsureValidEmployeeId(employeeId);
        Date = EnsureValidDate(date);
        Type = EnsureValidType(type);
        Comment = NormalizeComment(comment);
        Status = WorkAssignmentStatus.Active;
    }

    public static EmployeeWorkAssignment Create(
        Guid employeeId,
        DateOnly date,
        WorkAssignmentType type,
        string? comment)
        => new(
            Guid.NewGuid(),
            employeeId,
            date,
            type,
            comment);

    public void Update(
        DateOnly date,
        WorkAssignmentType type,
        string? comment)
    {
        EnsureIsActive();
        Date = EnsureValidDate(date);
        Type = EnsureValidType(type);
        Comment = NormalizeComment(comment);
    }

    public void Cancel()
    {
        if (Status == WorkAssignmentStatus.Cancelled)
        {
            return;
        }

        Status = WorkAssignmentStatus.Cancelled;
    }

    private void EnsureIsActive()
    {
        if (Status == WorkAssignmentStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Cancelled work assignments cannot be modified.");
        }
    }

    private static Guid EnsureValidEmployeeId(Guid employeeId)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "EmployeeId must be a non-empty GUID.",
                nameof(employeeId));
        }

        return employeeId;
    }

    private static DateOnly EnsureValidDate(DateOnly date)
    {
        if (date == default)
        {
            throw new ArgumentException(
                "Date must be a non-default value.",
                nameof(date));
        }

        return date;
    }

    private static WorkAssignmentType EnsureValidType(WorkAssignmentType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Unsupported work assignment type.");
        }

        return type;
    }

    private static string? NormalizeComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        var normalizedComment = comment.Trim();

        if (normalizedComment.Length > CommentMaxLength)
        {
            throw new ArgumentException(
                $"Comment cannot exceed {CommentMaxLength} characters.",
                nameof(comment));
        }

        return normalizedComment;
    }
}
