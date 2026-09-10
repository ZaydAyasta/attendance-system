using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Auditing.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;

namespace Attendance.Api.Tests.Infrastructure;

public sealed class IdentityApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    private static readonly Uri HttpsBaseAddress = new("https://localhost");

    public HttpClient CreateHttpsClient()
        => CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = HttpsBaseAddress,
            HandleCookies = true
        });

    public new HttpClient CreateClient(WebApplicationFactoryClientOptions options)
    {
        options.BaseAddress ??= HttpsBaseAddress;
        return base.CreateClient(options);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
            }));
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(x => x.ServiceType == typeof(DbContextOptions<AttendanceDbContext>));
            services.Remove(descriptor);
            services.AddDbContext<AttendanceDbContext>((serviceProvider, options) => options.UseNpgsql(connectionString)
                .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));
            services.PostConfigure<SecurityStampValidatorOptions>(options =>
                options.ValidationInterval = TimeSpan.Zero);
        });
    }
}
