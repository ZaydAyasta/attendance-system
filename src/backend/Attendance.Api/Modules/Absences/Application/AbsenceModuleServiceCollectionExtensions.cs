using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Api.Modules.Absences.Application;

public static class AbsenceModuleServiceCollectionExtensions
{
    public static IServiceCollection AddAbsencesModule(
        this IServiceCollection services)
    {
        services.AddScoped<AbsenceService>();

        return services;
    }
}
