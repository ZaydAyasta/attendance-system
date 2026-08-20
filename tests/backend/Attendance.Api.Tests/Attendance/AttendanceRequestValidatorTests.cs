using Attendance.Api.Modules.Attendance.Application;
using Xunit;

namespace Attendance.Api.Tests.Attendance;

public sealed class AttendanceRequestValidatorTests
{
    [Fact]
    public void ValidateRange_ReturnsErrorWhenFromIsAfterTo()
    {
        var result = AttendanceRequestValidator.ValidateRange(
            new DateOnly(2026, 8, 21),
            new DateOnly(2026, 8, 20));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("range"));
    }

    [Fact]
    public void ValidateRange_ReturnsErrorWhenSpanExceeds366Days()
    {
        var result = AttendanceRequestValidator.ValidateRange(
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 2));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("range"));
    }

    [Fact]
    public void ValidateRange_ReturnsErrorWhenFromIsMissing()
    {
        var result = AttendanceRequestValidator.ValidateRange(
            null,
            new DateOnly(2026, 8, 20));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("from"));
    }

    [Fact]
    public void ValidateRange_ReturnsErrorWhenToIsMissing()
    {
        var result = AttendanceRequestValidator.ValidateRange(
            new DateOnly(2026, 8, 20),
            null);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("to"));
    }
}
