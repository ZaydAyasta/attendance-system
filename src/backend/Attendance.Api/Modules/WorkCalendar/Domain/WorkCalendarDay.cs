namespace Attendance.Api.Modules.WorkCalendar.Domain;

/// <summary>
/// Represents the labor classification assigned to a specific calendar date.
/// </summary>
public sealed class WorkCalendarDay
{
    public const int DescriptionMaxLength = 500;

    /// <summary>
    /// Gets the unique identifier of the calendar day.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the calendar date being classified.
    /// </summary>
    public DateOnly Date { get; private set; }

    /// <summary>
    /// Gets the labor classification of the date.
    /// </summary>
    public DayType DayType { get; private set; }

    /// <summary>
    /// Gets an optional description associated with the date.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the PostgreSQL row version used for optimistic concurrency control.
    /// </summary>
    public uint Version { get; private set; }

    private WorkCalendarDay()
    {
    }

    private WorkCalendarDay(
        Guid id,
        DateOnly date,
        DayType dayType,
        string? description)
    {
        Id = id;
        Date = EnsureValidDate(date);
        DayType = EnsureValidDayType(dayType);
        Description = NormalizeDescription(description);
    }

    public static WorkCalendarDay Create(
        DateOnly date,
        DayType dayType,
        string? description)
        => new(Guid.NewGuid(), date, dayType, description);

    public void Update(
        DayType dayType,
        string? description)
    {
        DayType = EnsureValidDayType(dayType);
        Description = NormalizeDescription(description);
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

    private static DayType EnsureValidDayType(DayType dayType)
    {
        if (!Enum.IsDefined(dayType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dayType),
                dayType,
                "Unsupported work calendar day type.");
        }

        return dayType;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalizedDescription = description.Trim();

        if (normalizedDescription.Length > DescriptionMaxLength)
        {
            throw new ArgumentException(
                $"Description cannot exceed {DescriptionMaxLength} characters.",
                nameof(description));
        }

        return normalizedDescription;
    }
}
