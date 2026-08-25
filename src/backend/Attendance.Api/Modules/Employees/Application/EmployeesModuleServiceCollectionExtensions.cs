namespace Attendance.Api.Modules.Employees.Application;

public static class EmployeesModuleServiceCollectionExtensions
{
    public static IServiceCollection AddEmployeesModule(this IServiceCollection services)
    {
        services.AddScoped<EmployeeDirectoryService>();
        return services;
    }
}
