using Attendance.Api.Modules.WorkCalendar.Application;
using Attendance.Api.Modules.WorkCalendar.Contracts;
using Xunit;

namespace Attendance.Api.Tests.WorkCalendar;

public sealed class WorkCalendarRequestValidatorTests
{
    [Fact]
    public void ValidateRange_ReturnsErrorWhenFromIsGreaterThanTo()
    {
        var result = WorkCalendarRequestValidator.ValidateRange(
            new DateOnly(2026, 8, 31),
            new DateOnly(2026, 8, 30));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("range"));
    }

    [Fact]
    public void ValidateCreate_ReturnsErrorWhenDayTypeIsInvalid()
    {
        var result = WorkCalendarRequestValidator.ValidateCreate(
            new CreateWorkCalendarDayRequest(
                new DateOnly(2026, 8, 30),
                "Weekend",
                "Valor inválido"));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("dayType"));
    }

    [Fact]
    public void ValidateUpdate_ReturnsErrorWhenVersionIsMissing()
    {
        var result = WorkCalendarRequestValidator.ValidateUpdate(
            new UpdateWorkCalendarDayRequest(
                "WorkingDay",
                "Texto",
                0));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("version"));
    }
}
