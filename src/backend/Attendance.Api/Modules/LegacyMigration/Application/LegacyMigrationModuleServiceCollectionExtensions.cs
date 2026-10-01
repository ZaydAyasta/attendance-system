namespace Attendance.Api.Modules.LegacyMigration.Application;

public static class LegacyMigrationModuleServiceCollectionExtensions
{
    public static IServiceCollection AddLegacyMigrationModule(this IServiceCollection services)
    {
        services.AddScoped<LegacyProductionImportService>();
        return services;
    }
}
