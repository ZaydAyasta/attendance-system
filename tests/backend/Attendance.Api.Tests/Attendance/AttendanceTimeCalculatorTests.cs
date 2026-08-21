using Attendance.Api.Modules.Attendance.Domain;
using Xunit;

namespace Attendance.Api.Tests.Attendance;

public sealed class AttendanceTimeCalculatorTests
{
    private static readonly AttendanceTimeCalculator Calculator = new();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 8, 20);

    [Fact]
    public void Calculate_EntryAndExit_ReturnsCompleteWorkedTime()
    {
        var result = Calculate(
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertResult(result, 600, 0, 600, true);
    }

    [Fact]
    public void Calculate_WithLunchInterval_SubtractsLunchMinutes()
    {
        var result = Calculate(
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(13, 0, AttendanceMarkType.LunchStart),
            Mark(14, 0, AttendanceMarkType.LunchEnd),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertResult(result, 600, 60, 540, true);
    }

    [Fact]
    public void Calculate_CommissionInterval_DoesNotSubtractWorkedTime()
    {
        var result = Calculate(
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(10, 0, AttendanceMarkType.CommissionExit),
            Mark(12, 0, AttendanceMarkType.CommissionReturn),
            Mark(13, 30, AttendanceMarkType.LunchStart),
            Mark(14, 30, AttendanceMarkType.LunchEnd),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertResult(result, 600, 60, 540, true);
    }

    [Fact]
    public void Calculate_EntryOnly_ReturnsMissingExit()
    {
        var result = Calculate(Mark(8, 0, AttendanceMarkType.Entry));

        AssertIncomplete(result, lunchMinutes: 0, AttendanceTimeIssue.MissingExit);
    }

    [Fact]
    public void Calculate_ExitOnly_ReturnsMissingEntry()
    {
        var result = Calculate(Mark(18, 0, AttendanceMarkType.Exit));

        AssertIncomplete(result, lunchMinutes: 0, AttendanceTimeIssue.MissingEntry);
    }

    [Fact]
    public void Calculate_LunchStartWithoutLunchEnd_ReturnsIncomplete()
    {
        var result = Calculate(
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(13, 0, AttendanceMarkType.LunchStart),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertIncomplete(
            result,
            grossMinutes: 600,
            lunchMinutes: 0,
            AttendanceTimeIssue.MissingLunchEnd);
    }

    [Fact]
    public void Calculate_LunchEndWithoutLunchStart_ReturnsIncomplete()
    {
        var result = Calculate(
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(14, 0, AttendanceMarkType.LunchEnd),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertIncomplete(
            result,
            grossMinutes: 600,
            lunchMinutes: 0,
            AttendanceTimeIssue.LunchEndWithoutLunchStart);
    }

    [Fact]
    public void Calculate_MultipleEntries_ReturnsIncomplete()
    {
        var result = Calculate(
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(8, 5, AttendanceMarkType.Entry),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertIncomplete(result, lunchMinutes: 0, AttendanceTimeIssue.MultipleEntries);
    }

    [Fact]
    public void Calculate_MultipleExits_ReturnsIncomplete()
    {
        var result = Calculate(
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(17, 55, AttendanceMarkType.Exit),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertIncomplete(result, lunchMinutes: 0, AttendanceTimeIssue.MultipleExits);
    }

    [Fact]
    public void Calculate_UnorderedMarks_ProducesSameResult()
    {
        var result = Calculate(
            Mark(18, 0, AttendanceMarkType.Exit),
            Mark(14, 0, AttendanceMarkType.LunchEnd),
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(13, 0, AttendanceMarkType.LunchStart));

        AssertResult(result, 600, 60, 540, true);
    }

    [Fact]
    public void Calculate_OtherExitWithReturn_MarksResultAsUnresolved()
    {
        var result = Calculate(
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(10, 0, AttendanceMarkType.OtherExit),
            Mark(11, 0, AttendanceMarkType.OtherReturn),
            Mark(13, 30, AttendanceMarkType.LunchStart),
            Mark(14, 30, AttendanceMarkType.LunchEnd),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertIncomplete(
            result,
            grossMinutes: 600,
            lunchMinutes: 60,
            AttendanceTimeIssue.UnresolvedOtherExit);
    }

    [Fact]
    public void Calculate_OtherExitWithoutReturn_MarksResultAsUnresolved()
    {
        var result = Calculate(
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(15, 0, AttendanceMarkType.OtherExit),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertIncomplete(
            result,
            grossMinutes: 600,
            lunchMinutes: 0,
            AttendanceTimeIssue.UnresolvedOtherExit);
    }

    [Fact]
    public void Calculate_CommissionSequenceWithExit_ReturnsNormalWorkedTime()
    {
        var result = Calculate(
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(10, 0, AttendanceMarkType.CommissionExit),
            Mark(12, 0, AttendanceMarkType.CommissionReturn),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertResult(result, 600, 0, 600, true);
    }

    [Fact]
    public void Calculate_MultipleValidLunchIntervals_SumsResolvedMinutes()
    {
        var result = Calculate(
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(11, 0, AttendanceMarkType.LunchStart),
            Mark(11, 15, AttendanceMarkType.LunchEnd),
            Mark(14, 0, AttendanceMarkType.LunchStart),
            Mark(14, 30, AttendanceMarkType.LunchEnd),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertResult(result, 600, 45, 555, true);
    }

    [Fact]
    public void Calculate_InvertedLunchSequence_DoesNotProduceNegativeMinutes()
    {
        var result = Calculate(
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(13, 0, AttendanceMarkType.LunchEnd),
            Mark(14, 0, AttendanceMarkType.LunchStart),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertIncomplete(
            result,
            grossMinutes: 600,
            lunchMinutes: 0,
            AttendanceTimeIssue.LunchEndWithoutLunchStart,
            AttendanceTimeIssue.MissingLunchEnd);
    }

    [Fact]
    public void Calculate_NoMarks_ReturnsNoAttendanceMarksIssue()
    {
        var result = Calculate();

        AssertIncomplete(result, lunchMinutes: 0, AttendanceTimeIssue.NoAttendanceMarks);
    }

    [Fact]
    public void Calculate_MarksWithoutEntryOrExit_ReturnsMissingBoundaryIssues()
    {
        var result = Calculate(
            Mark(10, 0, AttendanceMarkType.CommissionExit),
            Mark(12, 0, AttendanceMarkType.CommissionReturn));

        AssertIncomplete(
            result,
            lunchMinutes: 0,
            AttendanceTimeIssue.MissingEntry,
            AttendanceTimeIssue.MissingExit);
    }

    [Fact]
    public void Calculate_LunchOutsidePrincipalWindow_IsIgnored()
    {
        var result = Calculate(
            Mark(7, 0, AttendanceMarkType.LunchStart),
            Mark(7, 30, AttendanceMarkType.LunchEnd),
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertResult(result, 600, 0, 600, true);
    }

    [Fact]
    public void Calculate_LunchExactlyWithinBoundaries_IsIncluded()
    {
        var result = Calculate(
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(8, 0, AttendanceMarkType.LunchStart),
            Mark(9, 0, AttendanceMarkType.LunchEnd),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertResult(result, 600, 60, 540, true);
    }

    [Fact]
    public void Calculate_ExitBeforeEntry_DoesNotProduceNegativeGrossMinutes()
    {
        var result = Calculate(
            Mark(18, 0, AttendanceMarkType.Entry),
            Mark(8, 0, AttendanceMarkType.Exit));

        AssertIncomplete(result, lunchMinutes: 0, AttendanceTimeIssue.ExitBeforeEntry);
        Assert.Null(result.GrossMinutes);
    }

    [Fact]
    public void Calculate_LunchSpansWholeWindow_DoesNotProduceNegativeWorkedMinutes()
    {
        var result = Calculate(
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(8, 0, AttendanceMarkType.LunchStart),
            Mark(18, 0, AttendanceMarkType.LunchEnd),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertResult(result, 600, 600, 0, true);
    }

    [Fact]
    public void Calculate_OtherReturnWithoutOtherExit_ReturnsIssue()
    {
        var result = Calculate(
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(11, 0, AttendanceMarkType.OtherReturn),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertIncomplete(
            result,
            grossMinutes: 600,
            lunchMinutes: 0,
            AttendanceTimeIssue.OtherReturnWithoutOtherExit);
    }

    [Fact]
    public void Calculate_OverlappingLunchIntervals_ReturnsIncomplete()
    {
        var result = Calculate(
            Mark(8, 0, AttendanceMarkType.Entry),
            Mark(12, 0, AttendanceMarkType.LunchStart),
            Mark(12, 30, AttendanceMarkType.LunchStart),
            Mark(13, 0, AttendanceMarkType.LunchEnd),
            Mark(18, 0, AttendanceMarkType.Exit));

        AssertIncomplete(
            result,
            grossMinutes: 600,
            lunchMinutes: 60,
            AttendanceTimeIssue.OverlappingLunch);
    }

    private static DailyWorkedTimeResult Calculate(
        params AttendanceMark[] marks)
        => Calculator.Calculate(marks);

    private static AttendanceMark Mark(
        int hour,
        int minute,
        AttendanceMarkType type)
        => AttendanceMark.Create(
            EmployeeId,
            new DateTimeOffset(
                Date.ToDateTime(new TimeOnly(hour, minute)),
                TimeSpan.Zero),
            type,
            AttendanceSource.Manual,
            checkpointId: null);

    private static void AssertResult(
        DailyWorkedTimeResult result,
        int? expectedGrossMinutes,
        int expectedLunchMinutes,
        int? expectedWorkedMinutes,
        bool expectedIsComplete)
    {
        Assert.Equal(expectedGrossMinutes, result.GrossMinutes);
        Assert.Equal(expectedLunchMinutes, result.LunchMinutes);
        Assert.Equal(expectedWorkedMinutes, result.WorkedMinutes);
        Assert.Equal(expectedIsComplete, result.IsComplete);
        Assert.Empty(result.Issues);
    }

    private static void AssertIncomplete(
        DailyWorkedTimeResult result,
        int lunchMinutes,
        params AttendanceTimeIssue[] expectedIssues)
        => AssertIncomplete(result, grossMinutes: null, lunchMinutes, expectedIssues);

    private static void AssertIncomplete(
        DailyWorkedTimeResult result,
        int? grossMinutes,
        int lunchMinutes,
        params AttendanceTimeIssue[] expectedIssues)
    {
        Assert.Equal(grossMinutes, result.GrossMinutes);
        Assert.Equal(lunchMinutes, result.LunchMinutes);
        Assert.Null(result.WorkedMinutes);
        Assert.False(result.IsComplete);
        Assert.Equal(expectedIssues, result.Issues);
    }
}
