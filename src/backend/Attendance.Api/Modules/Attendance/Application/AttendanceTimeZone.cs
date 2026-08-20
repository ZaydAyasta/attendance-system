namespace Attendance.Api.Modules.Attendance.Application;

public sealed class AttendanceTimeZone
{
    public const string ConfigurationSectionPath = "Attendance";

    public AttendanceTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new ArgumentException(
                "Attendance time zone must be configured.",
                nameof(timeZoneId));
        }

        TimeZoneId = timeZoneId.Trim();
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
    }

    public string TimeZoneId { get; }

    public TimeZoneInfo TimeZone { get; }

    public DateTimeOffset GetStartOfDay(DateOnly date)
    {
        var localDateTime = DateTime.SpecifyKind(
            date.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Unspecified);
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, TimeZone);

        return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
    }

    public DateTimeOffset GetStartOfNextDay(DateOnly date)
        => GetStartOfDay(date.AddDays(1));

    public DateOnly GetLocalDate(DateTimeOffset instant)
        => DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(instant, TimeZone).DateTime);
}
