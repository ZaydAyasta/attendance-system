using Attendance.Api.Modules.Absences.Application;
using Attendance.Api.Modules.Absences.Contracts;
using Xunit;

namespace Attendance.Api.Tests.Absences;

public sealed class AbsenceRequestValidatorTests
{
    [Fact]
    public void ValidateCreate_ReturnsErrorWhenRangeIsInvalid()
    {
        var result = AbsenceRequestValidator.ValidateCreate(
            new CreateAbsenceRequest(
                Guid.NewGuid(),
                new DateOnly(2026, 8, 20),
                new DateOnly(2026, 8, 10),
                "Vacation",
                "Approved",
                "Vacaciones",
                null));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("range"));
    }

    [Fact]
    public void ValidateUpdate_ReturnsErrorWhenVersionIsMissing()
    {
        var result = AbsenceRequestValidator.ValidateUpdate(
            new UpdateAbsenceRequest(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 12),
                "Vacation",
                "Approved",
                "Cambio",
                null,
                0));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("version"));
    }
}
