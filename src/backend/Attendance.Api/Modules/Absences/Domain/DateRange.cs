namespace Attendance.Api.Modules.Absences.Domain;

/// <summary>
/// Represents an inclusive range between two calendar dates.
/// </summary>
/// <param name="Start">The first date included in the range.</param>
/// <param name="End">The last date included in the range.</param>
public readonly record struct DateRange(DateOnly Start, DateOnly End)
{
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