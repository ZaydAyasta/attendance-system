using System.ComponentModel.DataAnnotations;

namespace Attendance.Api.Modules.Identity.Application;

internal static class OptionalEmail
{
    private static readonly EmailAddressAttribute Validator = new();

    public static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static bool IsValid(string? value)
        => value is null || Validator.IsValid(value);
}
