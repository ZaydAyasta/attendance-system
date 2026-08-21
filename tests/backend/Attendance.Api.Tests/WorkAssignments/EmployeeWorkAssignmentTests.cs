using Attendance.Api.Modules.WorkAssignments.Domain;
using Xunit;

namespace Attendance.Api.Tests.WorkAssignments;

public sealed class EmployeeWorkAssignmentTests
{
    [Fact]
    public void Create_ValidAssignment_StartsActive()
    {
        var employeeId = Guid.NewGuid();
        var date = new DateOnly(2026, 8, 22);

        var assignment = EmployeeWorkAssignment.Create(
            employeeId,
            date,
            WorkAssignmentType.WeekendWork,
            "Trabajo excepcional");

        Assert.Equal(employeeId, assignment.EmployeeId);
        Assert.Equal(date, assignment.Date);
        Assert.Equal(WorkAssignmentType.WeekendWork, assignment.Type);
        Assert.Equal("Trabajo excepcional", assignment.Comment);
        Assert.Equal(WorkAssignmentStatus.Active, assignment.Status);
    }

    [Fact]
    public void Create_EmptyEmployeeId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            EmployeeWorkAssignment.Create(
                Guid.Empty,
                new DateOnly(2026, 8, 22),
                WorkAssignmentType.WeekendWork,
                null));
    }

    [Fact]
    public void Create_DefaultDate_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            EmployeeWorkAssignment.Create(
                Guid.NewGuid(),
                default,
                WorkAssignmentType.WeekendWork,
                null));
    }

    [Fact]
    public void Create_InvalidType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EmployeeWorkAssignment.Create(
                Guid.NewGuid(),
                new DateOnly(2026, 8, 22),
                (WorkAssignmentType)999,
                null));
    }

    [Fact]
    public void Create_CommentTooLong_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            EmployeeWorkAssignment.Create(
                Guid.NewGuid(),
                new DateOnly(2026, 8, 22),
                WorkAssignmentType.WeekendWork,
                new string('x', EmployeeWorkAssignment.CommentMaxLength + 1)));
    }

    [Fact]
    public void Cancel_SetsCancelledStatus()
    {
        var assignment = EmployeeWorkAssignment.Create(
            Guid.NewGuid(),
            new DateOnly(2026, 8, 22),
            WorkAssignmentType.Recovery,
            null);

        assignment.Cancel();

        Assert.Equal(WorkAssignmentStatus.Cancelled, assignment.Status);
    }

    [Fact]
    public void Cancel_IsIdempotent()
    {
        var assignment = EmployeeWorkAssignment.Create(
            Guid.NewGuid(),
            new DateOnly(2026, 8, 22),
            WorkAssignmentType.Recovery,
            null);

        assignment.Cancel();
        assignment.Cancel();

        Assert.Equal(WorkAssignmentStatus.Cancelled, assignment.Status);
    }

    [Fact]
    public void Update_ActiveAssignment_ChangesValues()
    {
        var assignment = EmployeeWorkAssignment.Create(
            Guid.NewGuid(),
            new DateOnly(2026, 8, 22),
            WorkAssignmentType.WeekendWork,
            "Inicial");

        assignment.Update(
            new DateOnly(2026, 8, 23),
            WorkAssignmentType.TemporaryWork,
            "Actualizado");

        Assert.Equal(new DateOnly(2026, 8, 23), assignment.Date);
        Assert.Equal(WorkAssignmentType.TemporaryWork, assignment.Type);
        Assert.Equal("Actualizado", assignment.Comment);
        Assert.Equal(WorkAssignmentStatus.Active, assignment.Status);
    }

    [Fact]
    public void Update_CancelledAssignment_Throws()
    {
        var assignment = EmployeeWorkAssignment.Create(
            Guid.NewGuid(),
            new DateOnly(2026, 8, 22),
            WorkAssignmentType.WeekendWork,
            null);
        assignment.Cancel();

        Assert.Throws<InvalidOperationException>(() =>
            assignment.Update(
                new DateOnly(2026, 8, 23),
                WorkAssignmentType.Recovery,
                null));
    }
}
