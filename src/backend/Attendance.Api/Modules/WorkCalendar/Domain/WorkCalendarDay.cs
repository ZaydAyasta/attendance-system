namespace Attendance.Api.Modules.WorkCalendar.Domain;

/// <summary>
/// Represents the labor classification assigned to a specific calendar date.
/// </summary>
public sealed class WorkCalendarDay
{
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
}