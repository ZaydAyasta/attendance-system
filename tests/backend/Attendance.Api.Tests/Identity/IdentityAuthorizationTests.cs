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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
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

    [RequiresContainerRuntimeFact]
    public async Task Only_admin_can_list_identity_users()
    {
        await fixture.ResetAsync();
        using var factory = new IdentityApiFactory(fixture.ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateUserAsync(users, "admin", IdentityRoles.Admin, null);
        await CreateUserAsync(users, "user", IdentityRoles.User, null);
        await CreateUserAsync(users, "it", IdentityRoles.IT, null);

        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var user = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var it = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(admin, "admin", "Password1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(user, "user", "Password1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(it, "it", "Password1")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/identity/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/identity/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await it.GetAsync("/api/identity/users")).StatusCode);
    }

    [RequiresContainerRuntimeFact]
    public async Task Creating_user_requires_employee_and_prevents_duplicate_employee_account()
    {
        await fixture.ResetAsync();
        using var factory = new IdentityApiFactory(fixture.ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        var employee = Employee.Create("EMP-1", "Ana", "Torres", new DateOnly(2025, 1, 1));
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateUserAsync(users, "admin", IdentityRoles.Admin, null);
        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(admin, "admin", "Password1")).StatusCode);

        var missingEmployee = await SendWithCsrfAsync(admin, HttpMethod.Post, "/api/identity/users",
            new CreateIdentityUserRequest("new-user", null, "Password1", IdentityRoles.User, null));
        Assert.Equal(HttpStatusCode.BadRequest, missingEmployee.StatusCode);

        var create = await SendWithCsrfAsync(admin, HttpMethod.Post, "/api/identity/users",
            new CreateIdentityUserRequest("new-user", null, "Password1", IdentityRoles.User, employee.Id));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var duplicate = await SendWithCsrfAsync(admin, HttpMethod.Post, "/api/identity/users",
            new CreateIdentityUserRequest("other-user", null, "Password1", IdentityRoles.User, employee.Id));
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.Contains("Este empleado ya tiene una cuenta", await duplicate.Content.ReadAsStringAsync());
    }

    [RequiresContainerRuntimeFact]
    public async Task Disabled_account_cannot_sign_in_and_admin_cannot_disable_self()
    {
        await fixture.ResetAsync();
        using var factory = new IdentityApiFactory(fixture.ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateUserAsync(users, "admin", IdentityRoles.Admin, null);
        await CreateUserAsync(users, "disabled", IdentityRoles.IT, null);
        var disabled = await users.FindByNameAsync("disabled");
        Assert.NotNull(disabled);
        disabled!.LockoutEnd = DateTimeOffset.MaxValue;
        Assert.True((await users.UpdateAsync(disabled)).Succeeded);

        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(admin, "admin", "Password1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(factory.CreateClient(), "disabled", "Password1")).StatusCode);
        var me = await admin.GetFromJsonAsync<CurrentUserResponse>("/api/me");
        Assert.NotNull(me);
        var selfDisable = await SendWithCsrfAsync(admin, HttpMethod.Put, $"/api/identity/users/{me!.Id}/status", new SetIdentityUserStatusRequest(false));
        Assert.Equal(HttpStatusCode.BadRequest, selfDisable.StatusCode);
        Assert.Contains("No puedes desactivar tu propia cuenta", await selfDisable.Content.ReadAsStringAsync());
    }

    [RequiresContainerRuntimeFact]
    public async Task Reset_password_replaces_old_credential_and_role_update_is_reflected()
    {
        await fixture.ResetAsync();
        using var factory = new IdentityApiFactory(fixture.ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateUserAsync(users, "admin", IdentityRoles.Admin, null);
        await CreateUserAsync(users, "managed", IdentityRoles.IT, null);
        var managed = await users.FindByNameAsync("managed");
        Assert.NotNull(managed);
        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(admin, "admin", "Password1")).StatusCode);

        var reset = await SendWithCsrfAsync(admin, HttpMethod.Post, $"/api/identity/users/{managed!.Id}/reset-password", new ResetIdentityUserPasswordRequest("NewPassword1"));
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        using var oldPasswordClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var newPasswordClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(oldPasswordClient, "managed", "Password1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(newPasswordClient, "managed", "NewPassword1")).StatusCode);

        var update = await SendWithCsrfAsync(admin, HttpMethod.Put, $"/api/identity/users/{managed.Id}", new UpdateIdentityUserRequest("managed", null, IdentityRoles.Admin));
        var response = await update.Content.ReadFromJsonAsync<IdentityUserResponse>();
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.NotNull(response);
        Assert.Equal(IdentityRoles.Admin, response!.Role);
    }

    [RequiresContainerRuntimeFact]
    public async Task Remember_me_controls_cookie_persistence()
    {
        await fixture.ResetAsync();
        using var factory = new IdentityApiFactory(fixture.ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateUserAsync(users, "admin", IdentityRoles.Admin, null);
        using var remembered = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var sessionOnly = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var persistentLogin = await LoginAsync(remembered, "admin", "Password1", rememberMe: true);
        var sessionLogin = await LoginAsync(sessionOnly, "admin", "Password1", rememberMe: false);
        var persistentCookie = persistentLogin.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("attendance.auth=", StringComparison.OrdinalIgnoreCase));
        var sessionCookie = sessionLogin.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("attendance.auth=", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("expires=", persistentCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expires=", sessionCookie, StringComparison.OrdinalIgnoreCase);
    }

    [RequiresContainerRuntimeFact]
    public async Task Login_is_rate_limited_after_ten_attempts_from_the_same_client()
    {
        await fixture.ResetAsync();
        using var factory = new IdentityApiFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var response = await LoginAsync(client, $"unknown-{attempt}", "WrongPass1");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await LoginAsync(client, "unknown-final", "WrongPass1")).StatusCode);
    }

    [RequiresContainerRuntimeFact]
    public async Task Production_bootstrap_creates_one_administrator_only_when_explicitly_enabled()
    {
        await fixture.ResetAsync();
        using var factory = new IdentityApiFactory(fixture.ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:BootstrapAdmin:Enabled"] = "true",
            ["Identity:BootstrapAdmin:Username"] = "bootstrap-admin",
            ["Identity:BootstrapAdmin:Password"] = "BootstrapPass1"
        }).Build();
        var bootstrap = scope.ServiceProvider.GetRequiredService<ProductionAdminBootstrapService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.True(await bootstrap.BootstrapAsync(configuration));
        Assert.False(await bootstrap.BootstrapAsync(configuration));

        var administrator = await users.FindByNameAsync("bootstrap-admin");
        Assert.NotNull(administrator);
        Assert.Null(administrator!.Email);
        Assert.Null(administrator.NormalizedEmail);
        Assert.Contains(IdentityRoles.Admin, await users.GetRolesAsync(administrator!));
    }

    [RequiresContainerRuntimeFact]
    public async Task Production_bootstrap_rejects_an_explicit_invalid_email()
    {
        await fixture.ResetAsync();
        using var factory = new IdentityApiFactory(fixture.ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:BootstrapAdmin:Enabled"] = "true",
            ["Identity:BootstrapAdmin:Username"] = "bootstrap-admin",
            ["Identity:BootstrapAdmin:Password"] = "BootstrapPass1",
            ["Identity:BootstrapAdmin:Email"] = "not-an-email"
        }).Build();
        var bootstrap = scope.ServiceProvider.GetRequiredService<ProductionAdminBootstrapService>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrap.BootstrapAsync(configuration));

        Assert.Equal("Identity bootstrap email is invalid.", exception.Message);
    }

    [RequiresContainerRuntimeFact]
    public async Task Creating_user_normalizes_whitespace_email_to_null()
    {
        await fixture.ResetAsync();
        using var factory = new IdentityApiFactory(fixture.ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateUserAsync(users, "admin", IdentityRoles.Admin, null);
        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(admin, "admin", "Password1")).StatusCode);

        var response = await SendWithCsrfAsync(admin, HttpMethod.Post, "/api/identity/users",
            new CreateIdentityUserRequest("no-email", "   ", "Password1", IdentityRoles.IT, null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await users.FindByNameAsync("no-email");
        Assert.NotNull(created);
        Assert.Null(created!.Email);
        Assert.Null(created.NormalizedEmail);
    }

    [RequiresContainerRuntimeFact]
    public async Task Creating_user_rejects_an_explicit_invalid_email()
    {
        await fixture.ResetAsync();
        using var factory = new IdentityApiFactory(fixture.ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateUserAsync(users, "admin", IdentityRoles.Admin, null);
        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(admin, "admin", "Password1")).StatusCode);

        var response = await SendWithCsrfAsync(admin, HttpMethod.Post, "/api/identity/users",
            new CreateIdentityUserRequest("invalid-email", "not-an-email", "Password1", IdentityRoles.IT, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("El correo no es válido.", await response.Content.ReadAsStringAsync());
    }

    [RequiresContainerRuntimeFact]
    public async Task Database_rejects_duplicate_employee_and_email_identity_links()
    {
        await fixture.ResetAsync();
        using var factory = new IdentityApiFactory(fixture.ConnectionString);
        await using var firstScope = factory.Services.CreateAsyncScope();
        var firstDb = firstScope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        var employee = Employee.Create("EMP-UNIQUE", "Ana", "Torres", new DateOnly(2025, 1, 1));
        firstDb.Employees.Add(employee);
        await firstDb.SaveChangesAsync();
        var users = firstScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var first = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "first-user",
            Email = "shared@example.test",
            EmployeeId = employee.Id,
            LockoutEnabled = true
        };
        Assert.True((await users.CreateAsync(first, "Password1")).Succeeded);

        await using var duplicateEmployeeScope = factory.Services.CreateAsyncScope();
        var duplicateEmployeeDb = duplicateEmployeeScope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        duplicateEmployeeDb.Users.Add(DirectUser("second-user", "other@example.test", employee.Id));
        await Assert.ThrowsAsync<DbUpdateException>(() => duplicateEmployeeDb.SaveChangesAsync());

        await using var duplicateEmailScope = factory.Services.CreateAsyncScope();
        var duplicateEmailDb = duplicateEmailScope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        duplicateEmailDb.Users.Add(DirectUser("third-user", "shared@example.test", null));
        await Assert.ThrowsAsync<DbUpdateException>(() => duplicateEmailDb.SaveChangesAsync());
    }

    [RequiresContainerRuntimeFact]
    public async Task Disabling_an_account_invalidates_its_existing_session()
    {
        await fixture.ResetAsync();
        using var factory = new IdentityApiFactory(fixture.ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateUserAsync(users, "admin", IdentityRoles.Admin, null);
        await CreateUserAsync(users, "managed", IdentityRoles.IT, null);
        var managed = await users.FindByNameAsync("managed");
        Assert.NotNull(managed);

        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var managedClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(admin, "admin", "Password1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(managedClient, "managed", "Password1")).StatusCode);

        var disable = await SendWithCsrfAsync(admin, HttpMethod.Put, $"/api/identity/users/{managed!.Id}/status", new SetIdentityUserStatusRequest(false));

        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await managedClient.GetAsync("/api/me")).StatusCode);
    }

    [RequiresContainerRuntimeFact]
    public async Task Resetting_password_invalidates_the_existing_session()
    {
        await fixture.ResetAsync();
        using var factory = new IdentityApiFactory(fixture.ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateUserAsync(users, "admin", IdentityRoles.Admin, null);
        await CreateUserAsync(users, "managed", IdentityRoles.IT, null);
        var managed = await users.FindByNameAsync("managed");
        Assert.NotNull(managed);

        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var managedClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(admin, "admin", "Password1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(managedClient, "managed", "Password1")).StatusCode);

        var reset = await SendWithCsrfAsync(admin, HttpMethod.Post, $"/api/identity/users/{managed!.Id}/reset-password", new ResetIdentityUserPasswordRequest("NewPassword1"));

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await managedClient.GetAsync("/api/me")).StatusCode);
    }

    [RequiresContainerRuntimeFact]
    public async Task Changing_role_refreshes_the_existing_session_claims()
    {
        await fixture.ResetAsync();
        using var factory = new IdentityApiFactory(fixture.ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateUserAsync(users, "admin", IdentityRoles.Admin, null);
        await CreateUserAsync(users, "managed", IdentityRoles.IT, null);
        var managed = await users.FindByNameAsync("managed");
        Assert.NotNull(managed);

        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var managedClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(admin, "admin", "Password1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(managedClient, "managed", "Password1")).StatusCode);

        var update = await SendWithCsrfAsync(admin, HttpMethod.Put, $"/api/identity/users/{managed!.Id}", new UpdateIdentityUserRequest("managed", null, IdentityRoles.Admin));

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await managedClient.GetAsync("/api/identity/users")).StatusCode);
    }

    private static async Task CreateUserAsync(UserManager<ApplicationUser> users, string username, string role, Guid? employeeId)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = username, EmployeeId = employeeId, LockoutEnabled = true };
        var result = await users.CreateAsync(user, "Password1");
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(x => x.Description)));
        Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string username, string password, bool rememberMe = false)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/auth/csrf");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new LoginRequest(username, password, rememberMe)) };
        request.Headers.Add("X-CSRF-TOKEN", token!.Token);
        return await client.SendAsync(request);
    }

    private static ApplicationUser DirectUser(string username, string email, Guid? employeeId) => new()
    {
        Id = Guid.NewGuid(),
        UserName = username,
        NormalizedUserName = username.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        SecurityStamp = Guid.NewGuid().ToString(),
        ConcurrencyStamp = Guid.NewGuid().ToString(),
        EmployeeId = employeeId,
        LockoutEnabled = true
    };

    private static async Task<HttpResponseMessage> SendWithCsrfAsync(HttpClient client, HttpMethod method, string path, object body)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/auth/csrf");
        var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
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
