using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Api.Modules.WorkAssignments.Application;

public static class WorkAssignmentModuleServiceCollectionExtensions
{
    public static IServiceCollection AddWorkAssignmentsModule(
        this IServiceCollection services)
    {
        services.AddScoped<WorkAssignmentService>();

        return services;
    }
}
