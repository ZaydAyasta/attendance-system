using System.Net;
using System.Net.Http.Json;
using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.Identity.Application;
using Attendance.Api.Modules.Identity.Contracts;
using Attendance.Api.Modules.Identity.Domain;
using Attendance.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Attendance.Api.Tests.Identity;

public sealed class IdentityAuthorizationTests(PostgreSqlAttendanceDatabaseFixture fixture)
    : IClassFixture<PostgreSqlAttendanceDatabaseFixture>
{
    [RequiresContainerRuntimeFact]
    public async Task Anonymous_admin_user_and_it_access_are_enforced_by_backend_policies()
    {
        await fixture.ResetAsync();
        using var factory = new IdentityApiFactory(fixture.ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        var employeeA = Employee.Create("EMP-A", "Ana", "Torres", new DateOnly(2025, 1, 1));
        var employeeB = Employee.Create("EMP-B", "Bruno", "Díaz", new DateOnly(2025, 1, 1));
        db.Employees.AddRange(employeeA, employeeB);
        await db.SaveChangesAsync();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateUserAsync(users, "admin", IdentityRoles.Admin, null);
        await CreateUserAsync(users, "user-a", IdentityRoles.User, employeeA.Id);
        await CreateUserAsync(users, "it", IdentityRoles.IT, null);

        using var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/employees?isActive=true")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(anonymous, "wrong", "WrongPass1")).StatusCode);

        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(admin, "admin", "Password1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/employees?isActive=true")).StatusCode);

        using var user = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(user, "user-a", "Password1")).StatusCode);
        var me = await user.GetFromJsonAsync<CurrentUserResponse>("/api/me");
        Assert.NotNull(me);
        Assert.Equal(employeeA.Id, me!.EmployeeId);
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/me/attendance?from=2026-08-01&to=2026-08-02")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/me/absences")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync($"/api/employees/{employeeB.Id}/attendance?from=2026-08-01&to=2026-08-02")).StatusCode);

        using var it = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(it, "it", "Password1")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await it.GetAsync("/api/employees?isActive=true")).StatusCode);

        var logout = await LogoutAsync(admin);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await admin.GetAsync("/api/me")).StatusCode);
    }

    private static async Task CreateUserAsync(UserManager<ApplicationUser> users, string username, string role, Guid? employeeId)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = username, EmployeeId = employeeId, LockoutEnabled = true };
        var result = await users.CreateAsync(user, "Password1");
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(x => x.Description)));
        Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string username, string password)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/auth/csrf");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new LoginRequest(username, password)) };
        request.Headers.Add("X-CSRF-TOKEN", token!.Token);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> LogoutAsync(HttpClient client)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/auth/csrf");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Add("X-CSRF-TOKEN", token!.Token);
        return await client.SendAsync(request);
    }
}
