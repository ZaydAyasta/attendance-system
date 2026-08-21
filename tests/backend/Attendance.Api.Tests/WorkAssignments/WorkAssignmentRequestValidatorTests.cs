using Attendance.Api.Modules.WorkAssignments.Application;
using Attendance.Api.Modules.WorkAssignments.Contracts;
using Attendance.Api.Modules.WorkAssignments.Domain;
using Xunit;

namespace Attendance.Api.Tests.WorkAssignments;

public sealed class WorkAssignmentRequestValidatorTests
{
    [Fact]
    public void ValidateCreate_ReturnsErrorWhenDateIsDefault()
    {
        var result = WorkAssignmentRequestValidator.ValidateCreate(
            new CreateWorkAssignmentRequest(
                Guid.NewGuid(),
                default,
                "WeekendWork",
                null));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("date"));
    }

    [Fact]
    public void ValidateCreate_ReturnsErrorWhenTypeIsInvalid()
    {
        var result = WorkAssignmentRequestValidator.ValidateCreate(
            new CreateWorkAssignmentRequest(
                Guid.NewGuid(),
                new DateOnly(2026, 8, 22),
                "Unexpected",
                null));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("type"));
    }

    [Fact]
    public void ValidateUpdate_ReturnsErrorWhenVersionIsMissing()
    {
        var result = WorkAssignmentRequestValidator.ValidateUpdate(
            new UpdateWorkAssignmentRequest(
                new DateOnly(2026, 8, 22),
                "WeekendWork",
                null,
                0));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("version"));
    }

    [Theory]
    [InlineData("Active", WorkAssignmentStatus.Active)]
    [InlineData("Cancelled", WorkAssignmentStatus.Cancelled)]
    public void ValidateList_ParsesSupportedStatuses(
        string rawStatus,
        WorkAssignmentStatus expectedStatus)
    {
        var result = WorkAssignmentRequestValidator.ValidateList(
            null,
            null,
            null,
            rawStatus,
            null);

        Assert.True(result.IsValid);
        Assert.Equal(expectedStatus, result.Value!.Status);
    }
}
