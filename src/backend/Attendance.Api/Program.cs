using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Absences.Application;
using Attendance.Api.Modules.Absences.Endpoints;
using Attendance.Api.Modules.Employees.Application;
using Attendance.Api.Modules.Employees.Endpoints;
using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.Attendance.Application;
using Attendance.Api.Modules.Attendance.Endpoints;
using Attendance.Api.Modules.WorkCalendar.Application;
using Attendance.Api.Modules.WorkCalendar.Endpoints;
using Attendance.Api.Modules.WorkAssignments.Application;
using Attendance.Api.Modules.WorkAssignments.Endpoints;
using Attendance.Api.Modules.Identity.Application;
using Attendance.Api.Modules.Identity.Domain;
using Attendance.Api.Modules.Identity.Endpoints;
using Attendance.Api.Modules.Checkpoints.Application;
using Attendance.Api.Modules.Checkpoints.Endpoints;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<AttendanceDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
})
    .AddEntityFrameworkStores<AttendanceDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "attendance.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(IdentityRoles.Admin));
    options.AddPolicy("EmployeeSelfService", policy => policy.RequireRole(IdentityRoles.User));
    options.AddPolicy("ITOnly", policy => policy.RequireRole(IdentityRoles.IT));
});
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddScoped<IdentitySessionService>();
builder.Services.AddScoped<IdentityUserAdministrationService>();
builder.Services.AddAbsencesModule();
builder.Services.AddEmployeesModule();
builder.Services.AddAttendanceModule(builder.Configuration);
builder.Services.AddWorkCalendarModule();
builder.Services.AddWorkAssignmentsModule();
builder.Services.AddCheckpointsModule();

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info ??= new OpenApiInfo();
        document.Info.Title = "Sistema de Asistencia API";
        document.Info.Version = "v1";

        return Task.CompletedTask;
    });
});

var app = builder.Build();

await using (var identityScope = app.Services.CreateAsyncScope())
{
    await EnsureIdentityRolesAsync(identityScope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
    var seedEmployees = new[]
    {
        (Guid.Parse("b89f8253-79bb-4bb2-98d1-67114482fe01"), "EMP-TEST-001", "María", "Torres"),
        (Guid.Parse("b89f8253-79bb-4bb2-98d1-67114482fe02"), "EMP-TEST-002", "Carlos", "Ramírez"),
        (Guid.Parse("b89f8253-79bb-4bb2-98d1-67114482fe03"), "EMP-TEST-003", "Lucía", "Mendoza"),
        (Guid.Parse("b89f8253-79bb-4bb2-98d1-67114482fe04"), "EMP-TEST-004", "Diego", "Vargas")
    };

    var seedCodes = seedEmployees.Select(seed => seed.Item2).ToArray();
    var existingCodes = await dbContext.Employees
        .Where(x => seedCodes.Contains(x.EmployeeCode))
        .Select(x => x.EmployeeCode)
        .ToListAsync();

    foreach (var (id, code, firstName, lastName) in seedEmployees.Where(seed => !existingCodes.Contains(seed.Item2)))
    {
        var employee = (Employee)Activator.CreateInstance(typeof(Employee), nonPublic: true)!;
        dbContext.Employees.Add(employee);
        dbContext.Entry(employee).Property(x => x.Id).CurrentValue = id;
        dbContext.Entry(employee).Property(x => x.EmployeeCode).CurrentValue = code;
        dbContext.Entry(employee).Property(x => x.FirstName).CurrentValue = firstName;
        dbContext.Entry(employee).Property(x => x.LastName).CurrentValue = lastName;
        dbContext.Entry(employee).Property(x => x.IsActive).CurrentValue = true;
        dbContext.Entry(employee).Property(x => x.HireDate).CurrentValue = new DateOnly(2025, 1, 1);
    }

    await dbContext.SaveChangesAsync();

    await SeedIdentityAsync(scope.ServiceProvider, builder.Configuration);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs", options =>
    {
        options.WithTitle("Sistema de Asistencia API");
        options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapIdentityEndpoints();
app.MapAbsenceEndpoints();
app.MapEmployeeEndpoints();
app.MapAttendanceEndpoints();
app.MapWorkCalendarEndpoints();
app.MapWorkAssignmentEndpoints();
app.MapCheckpointEndpoints();

app.Run();

static async Task SeedIdentityAsync(IServiceProvider services, IConfiguration configuration)
{
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    foreach (var role in IdentityRoles.All)
    {
        var username = configuration[$"Identity:SeedUsers:{role}:Username"];
        var password = configuration[$"Identity:SeedUsers:{role}:Password"];
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || await userManager.FindByNameAsync(username) is not null)
        {
            continue;
        }
        Guid? employeeId = null;
        var configuredEmployeeId = configuration[$"Identity:SeedUsers:{role}:EmployeeId"];
        if (Guid.TryParse(configuredEmployeeId, out var parsedEmployeeId)) employeeId = parsedEmployeeId;
        if (role == IdentityRoles.User && employeeId is null) continue;
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = username, EmployeeId = employeeId, LockoutEnabled = true };
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded) await userManager.AddToRoleAsync(user, role);
    }
}

static async Task EnsureIdentityRolesAsync(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var role in IdentityRoles.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }

}

public partial class Program;
