namespace Attendance.Api.Modules.Attendance.Domain;

/// <summary>
/// Represents the daily worked-time calculation derived from attendance marks.
/// </summary>
public sealed class DailyWorkedTimeResult
{
    /// <summary>
    /// Gets the whole minutes between the unique principal <see cref="AttendanceMarkType.Entry"/>
    /// and <see cref="AttendanceMarkType.Exit"/> marks, before subtracting lunch.
    /// </summary>
    public int? GrossMinutes { get; }

    /// <summary>
    /// Gets the whole minutes from resolved <see cref="AttendanceMarkType.LunchStart"/>
    /// to <see cref="AttendanceMarkType.LunchEnd"/> intervals contained in the principal window.
    /// </summary>
    public int LunchMinutes { get; }

    /// <summary>
    /// Gets the final worked whole minutes after subtracting resolved lunch intervals.
    /// This value is <see langword="null"/> when the sequence is incomplete, ambiguous,
    /// or contains <see cref="AttendanceMarkType.OtherExit"/> intervals whose policy is unresolved.
    /// Commission intervals are never subtracted.
    /// </summary>
    public int? WorkedMinutes { get; }

    public bool IsComplete { get; }

    public IReadOnlyCollection<AttendanceTimeIssue> Issues { get; }

    public DailyWorkedTimeResult(
        int? grossMinutes,
        int lunchMinutes,
        int? workedMinutes,
        bool isComplete,
        IReadOnlyCollection<AttendanceTimeIssue> issues)
    {
        if (grossMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(grossMinutes),
                grossMinutes,
                "GrossMinutes cannot be negative.");
        }

        if (lunchMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lunchMinutes),
                lunchMinutes,
                "LunchMinutes cannot be negative.");
        }

        if (workedMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(workedMinutes),
                workedMinutes,
                "WorkedMinutes cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(issues);

        GrossMinutes = grossMinutes;
        LunchMinutes = lunchMinutes;
        WorkedMinutes = workedMinutes;
        IsComplete = isComplete;
        Issues = issues;
    }
}
