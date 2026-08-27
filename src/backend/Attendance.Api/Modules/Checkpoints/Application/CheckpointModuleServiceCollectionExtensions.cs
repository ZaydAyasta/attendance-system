using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Api.Modules.Checkpoints.Application;

public static class CheckpointModuleServiceCollectionExtensions
{
    public static IServiceCollection AddCheckpointsModule(this IServiceCollection services)
    {
        services.AddDataProtection();
        services.AddSingleton<CheckpointQrService>();
        services.AddScoped<CheckpointService>();
        return services;
    }
}
