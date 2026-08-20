using Attendance.Api.Modules.Attendance.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Api.Modules.Attendance.Application;

public static class AttendanceModuleServiceCollectionExtensions
{
    public static IServiceCollection AddAttendanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var timeZoneId =
            configuration[$"{AttendanceTimeZone.ConfigurationSectionPath}:TimeZone"]
            ?? "America/Lima";

        services.AddSingleton(new AttendanceTimeZone(timeZoneId));
        services.AddSingleton<AttendanceEvaluator>();
        services.AddScoped<DailyAttendanceService>();

        return services;
    }
}
