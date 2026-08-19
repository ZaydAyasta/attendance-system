namespace Attendance.Api.Modules.Absences.Domain;

/// <summary>
/// Represents an inclusive range between two calendar dates.
/// </summary>
public readonly record struct DateRange
{
    public DateRange(DateOnly start, DateOnly end)
    {
        if (start == default)
        {
            throw new ArgumentOutOfRangeException(
                nameof(start),
                "Start date must be a non-default value.");
        }

        if (end == default)
        {
            throw new ArgumentOutOfRangeException(
                nameof(end),
                "End date must be a non-default value.");
        }

        if (start > end)
        {
            throw new ArgumentOutOfRangeException(
                nameof(start),
                "Start date must be less than or equal to end date.");
        }

        Start = start;
        End = end;
    }

    public DateOnly Start { get; }

    public DateOnly End { get; }

    /// <summary>
    /// Determines whether the specified date belongs to this range.
    /// </summary>
    public bool Contains(DateOnly date)
        => date >= Start && date <= End;

    /// <summary>
    /// Determines whether this range overlaps another range.
    /// </summary>
    public bool Overlaps(DateRange other)
        => Start <= other.End && End >= other.Start;
}
