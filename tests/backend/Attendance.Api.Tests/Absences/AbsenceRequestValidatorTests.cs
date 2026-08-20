using System.Reflection;
using Attendance.Api.Modules.Absences.Application;
using Attendance.Api.Modules.Absences.Contracts;
using Attendance.Api.Modules.Absences.Domain;
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
                "Cambio",
                null,
                0));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("version"));
    }

    [Fact]
    public void CreateAbsenceRequest_DoesNotExposeStatus()
    {
        var statusProperty = typeof(CreateAbsenceRequest)
            .GetProperty(
                "Status",
                BindingFlags.Instance | BindingFlags.Public);

        Assert.Null(statusProperty);
    }

    [Fact]
    public void UpdateAbsenceRequest_DoesNotExposeStatus()
    {
        var statusProperty = typeof(UpdateAbsenceRequest)
            .GetProperty(
                "Status",
                BindingFlags.Instance | BindingFlags.Public);

        Assert.Null(statusProperty);
    }

    [Theory]
    [InlineData("Active", AbsenceStatus.Active)]
    [InlineData("Cancelled", AbsenceStatus.Cancelled)]
    public void ValidateList_ParsesSupportedStatuses(
        string rawStatus,
        AbsenceStatus expectedStatus)
    {
        var result = AbsenceRequestValidator.ValidateList(
            null,
            null,
            null,
            rawStatus,
            null);

        Assert.True(result.IsValid);
        Assert.Equal(expectedStatus, result.Value!.Status);
    }

    [Fact]
    public void ValidateList_RejectsLegacyStatuses()
    {
        var result = AbsenceRequestValidator.ValidateList(
            null,
            null,
            null,
            "Approved",
            null);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("status"));
    }
}
