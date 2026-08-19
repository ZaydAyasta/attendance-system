using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Api.Modules.WorkCalendar.Application;

public static class WorkCalendarModuleServiceCollectionExtensions
{
    public static IServiceCollection AddWorkCalendarModule(
        this IServiceCollection services)
    {
        services.AddScoped<WorkCalendarService>();

        return services;
    }
}
