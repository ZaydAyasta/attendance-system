namespace Attendance.Api.Modules.Attendance.Domain;

/// <summary>
/// Calculates daily worked time from raw attendance marks without consulting external systems.
/// </summary>
public sealed class AttendanceTimeCalculator
{
    public DailyWorkedTimeResult Calculate(
        IReadOnlyCollection<AttendanceMark> marks)
    {
        ArgumentNullException.ThrowIfNull(marks);

        if (marks.Count == 0)
        {
            return Incomplete(
                grossMinutes: null,
                lunchMinutes: 0,
                AttendanceTimeIssue.NoAttendanceMarks);
        }

        var orderedMarks = marks
            .OrderBy(x => x.OccurredAt)
            .ToArray();
        var issues = new List<AttendanceTimeIssue>();

        var entryMarks = orderedMarks
            .Where(x => x.Type == AttendanceMarkType.Entry)
            .ToArray();
        var exitMarks = orderedMarks
            .Where(x => x.Type == AttendanceMarkType.Exit)
            .ToArray();

        ValidatePrincipalWindowMarkers(entryMarks, exitMarks, issues);

        if (entryMarks.Length != 1 || exitMarks.Length != 1)
        {
            return Incomplete(
                grossMinutes: null,
                lunchMinutes: 0,
                issues);
        }

        var entryMark = entryMarks[0];
        var exitMark = exitMarks[0];

        if (exitMark.OccurredAt < entryMark.OccurredAt)
        {
            AddIssue(issues, AttendanceTimeIssue.ExitBeforeEntry);

            return Incomplete(
                grossMinutes: null,
                lunchMinutes: 0,
                issues);
        }

        var grossMinutes = ToWholeMinutes(exitMark.OccurredAt - entryMark.OccurredAt);
        var lunchMinutes = 0;
        var canCalculateWorkedMinutes = true;
        DateTimeOffset? activeLunchStart = null;
        DateTimeOffset? activeOtherExit = null;

        foreach (var mark in orderedMarks)
        {
            if (mark.OccurredAt < entryMark.OccurredAt
                || mark.OccurredAt > exitMark.OccurredAt)
            {
                continue;
            }

            switch (mark.Type)
            {
                case AttendanceMarkType.Entry:
                case AttendanceMarkType.Exit:
                case AttendanceMarkType.CommissionExit:
                case AttendanceMarkType.CommissionReturn:
                    break;
                case AttendanceMarkType.LunchStart:
                    if (activeLunchStart is not null)
                    {
                        AddIssue(issues, AttendanceTimeIssue.OverlappingLunch);
                        canCalculateWorkedMinutes = false;
                        break;
                    }

                    activeLunchStart = mark.OccurredAt;
                    break;
                case AttendanceMarkType.LunchEnd:
                    if (activeLunchStart is null)
                    {
                        AddIssue(issues, AttendanceTimeIssue.LunchEndWithoutLunchStart);
                        canCalculateWorkedMinutes = false;
                        break;
                    }

                    lunchMinutes += ToWholeMinutes(mark.OccurredAt - activeLunchStart.Value);
                    activeLunchStart = null;
                    break;
                case AttendanceMarkType.OtherExit:
                    AddIssue(issues, AttendanceTimeIssue.UnresolvedOtherExit);
                    canCalculateWorkedMinutes = false;

                    if (activeOtherExit is null)
                    {
                        activeOtherExit = mark.OccurredAt;
                    }

                    break;
                case AttendanceMarkType.OtherReturn:
                    if (activeOtherExit is null)
                    {
                        AddIssue(issues, AttendanceTimeIssue.OtherReturnWithoutOtherExit);
                        canCalculateWorkedMinutes = false;
                        break;
                    }

                    activeOtherExit = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(mark.Type),
                        mark.Type,
                        "Unsupported attendance mark type.");
            }
        }

        if (activeLunchStart is not null)
        {
            AddIssue(issues, AttendanceTimeIssue.MissingLunchEnd);
            canCalculateWorkedMinutes = false;
        }

        if (activeOtherExit is not null)
        {
            AddIssue(issues, AttendanceTimeIssue.UnresolvedOtherExit);
            canCalculateWorkedMinutes = false;
        }

        if (!canCalculateWorkedMinutes)
        {
            return Incomplete(grossMinutes, lunchMinutes, issues);
        }

        var workedMinutes = Math.Max(grossMinutes - lunchMinutes, 0);

        return new DailyWorkedTimeResult(
            grossMinutes,
            lunchMinutes,
            workedMinutes,
            isComplete: true,
            Array.Empty<AttendanceTimeIssue>());
    }

    private static void ValidatePrincipalWindowMarkers(
        AttendanceMark[] entryMarks,
        AttendanceMark[] exitMarks,
        List<AttendanceTimeIssue> issues)
    {
        if (entryMarks.Length == 0)
        {
            AddIssue(issues, AttendanceTimeIssue.MissingEntry);
        }
        else if (entryMarks.Length > 1)
        {
            AddIssue(issues, AttendanceTimeIssue.MultipleEntries);
        }

        if (exitMarks.Length == 0)
        {
            AddIssue(issues, AttendanceTimeIssue.MissingExit);
        }
        else if (exitMarks.Length > 1)
        {
            AddIssue(issues, AttendanceTimeIssue.MultipleExits);
        }
    }

    private static DailyWorkedTimeResult Incomplete(
        int? grossMinutes,
        int lunchMinutes,
        params AttendanceTimeIssue[] issues)
        => Incomplete(
            grossMinutes,
            lunchMinutes,
            (IReadOnlyCollection<AttendanceTimeIssue>)issues);

    private static DailyWorkedTimeResult Incomplete(
        int? grossMinutes,
        int lunchMinutes,
        IReadOnlyCollection<AttendanceTimeIssue> issues)
        => new(
            grossMinutes,
            lunchMinutes,
            workedMinutes: null,
            isComplete: false,
            issues);

    private static void AddIssue(
        ICollection<AttendanceTimeIssue> issues,
        AttendanceTimeIssue issue)
    {
        if (!issues.Contains(issue))
        {
            issues.Add(issue);
        }
    }

    private static int ToWholeMinutes(TimeSpan value)
        => Math.Max((int)value.TotalMinutes, 0);
}
