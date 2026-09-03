using System.Net;
using System.Net.Http.Json;
using System.Text;
using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Auditing.Contracts;
using Attendance.Api.Modules.Employees.Contracts;
using Attendance.Api.Modules.Absences.Contracts;
using Attendance.Api.Modules.WorkCalendar.Contracts;
using Attendance.Api.Modules.Identity.Application;
using Attendance.Api.Modules.Identity.Contracts;
using Attendance.Api.Modules.Identity.Domain;
using Attendance.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Attendance.Api.Tests.Auditing;

public sealed class AuditEndpointTests(PostgreSqlAttendanceDatabaseFixture fixture) : IClassFixture<PostgreSqlAttendanceDatabaseFixture>
{
    [RequiresContainerRuntimeFact]
    public async Task Admin_can_list_audit_events_but_user_and_it_cannot()
    {
        await fixture.ResetAsync(); using var factory = new IdentityApiFactory(fixture.ConnectionString); await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateUserAsync(users, "admin", IdentityRoles.Admin); await CreateUserAsync(users, "user", IdentityRoles.User); await CreateUserAsync(users, "it", IdentityRoles.IT);
        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true }); using var user = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true }); using var it = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(admin, "admin"); await LoginAsync(user, "user"); await LoginAsync(it, "it");
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/audit-events")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/audit-events")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await it.GetAsync("/api/audit-events")).StatusCode);
    }

    [RequiresContainerRuntimeFact]
    public async Task Create_employee_records_authenticated_actor_and_safe_metadata()
    {
        await fixture.ResetAsync(); using var factory = new IdentityApiFactory(fixture.ConnectionString); await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); await CreateUserAsync(users, "admin", IdentityRoles.Admin);
        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true }); await LoginAsync(admin, "admin");
        var response = await SendWithCsrfAsync(admin, HttpMethod.Post, "/api/employees", new CreateEmployeeRequest("AUD-001", "Ana", "Audit", new DateOnly(2025, 1, 1)));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await admin.GetFromJsonAsync<PagedAuditEventsResponse>("/api/audit-events?action=CreateEmployee");
        var audit = Assert.Single(page!.Items);
        Assert.Equal("admin", audit.Actor.DisplayName); Assert.Equal("Employee", audit.EntityType); Assert.Contains("AUD-001", audit.Metadata!.Value.GetRawText());
        Assert.DoesNotContain("password", audit.Metadata!.Value.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [RequiresContainerRuntimeFact]
    public async Task Audit_events_are_paginated_and_returned_newest_first()
    {
        await fixture.ResetAsync(); using var factory = new IdentityApiFactory(fixture.ConnectionString); await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); await CreateUserAsync(users, "admin", IdentityRoles.Admin);
        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true }); await LoginAsync(admin, "admin");
        await SendWithCsrfAsync(admin, HttpMethod.Post, "/api/employees", new CreateEmployeeRequest("AUD-001", "Ana", "Audit", new DateOnly(2025, 1, 1)));
        await SendWithCsrfAsync(admin, HttpMethod.Post, "/api/employees", new CreateEmployeeRequest("AUD-002", "Bruno", "Audit", new DateOnly(2025, 1, 1)));
        var page = await admin.GetFromJsonAsync<PagedAuditEventsResponse>("/api/audit-events?page=1&pageSize=1");
        Assert.Equal(2, page!.Total); Assert.Single(page.Items); Assert.True(page.Items[0].OccurredAt >= (await admin.GetFromJsonAsync<PagedAuditEventsResponse>("/api/audit-events?page=2&pageSize=1"))!.Items[0].OccurredAt);
    }

    [RequiresContainerRuntimeFact]
    public async Task Update_employee_and_cancel_absence_each_create_an_audit_event()
    {
        await fixture.ResetAsync(); using var factory = new IdentityApiFactory(fixture.ConnectionString); await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); await CreateUserAsync(users, "admin", IdentityRoles.Admin);
        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true }); await LoginAsync(admin, "admin");
        var created = await SendWithCsrfAsync(admin, HttpMethod.Post, "/api/employees", new CreateEmployeeRequest("AUD-003", "Carla", "Audit", new DateOnly(2025, 1, 1)));
        var employee = await created.Content.ReadFromJsonAsync<EmployeeResponse>(); Assert.NotNull(employee);
        Assert.Equal(HttpStatusCode.OK, (await SendWithCsrfAsync(admin, HttpMethod.Put, $"/api/employees/{employee!.Id}", new UpdateEmployeeRequest("AUD-003", "Carla", "Actualizada", new DateOnly(2025, 1, 1), employee.Version))).StatusCode);
        var absenceResponse = await SendWithCsrfAsync(admin, HttpMethod.Post, "/api/absences", new CreateAbsenceRequest(employee.Id, new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 11), "Vacation", null, null));
        var absence = await absenceResponse.Content.ReadFromJsonAsync<AbsenceResponse>(); Assert.NotNull(absence);
        Assert.Equal(HttpStatusCode.NoContent, (await SendWithCsrfAsync(admin, HttpMethod.Post, $"/api/absences/{absence!.Id}/cancel", new CancelAbsenceRequest(absence.Version))).StatusCode);
        var events = await admin.GetFromJsonAsync<PagedAuditEventsResponse>("/api/audit-events?pageSize=50");
        Assert.Contains(events!.Items, x => x.Action == "UpdateEmployee"); Assert.Contains(events.Items, x => x.Action == "CancelAbsence");
    }

    [RequiresContainerRuntimeFact]
    public async Task Bulk_calendar_creates_one_summary_event_and_password_reset_never_stores_password()
    {
        await fixture.ResetAsync(); using var factory = new IdentityApiFactory(fixture.ConnectionString); await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); await CreateUserAsync(users, "admin", IdentityRoles.Admin); await CreateUserAsync(users, "target", IdentityRoles.IT);
        var target = await users.FindByNameAsync("target"); Assert.NotNull(target);
        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true }); await LoginAsync(admin, "admin");
        var days = new[] { new BulkConfigureWorkCalendarDayRequest(new DateOnly(2026, 2, 1), "WorkingDay", null, null), new BulkConfigureWorkCalendarDayRequest(new DateOnly(2026, 2, 2), "WorkingDay", null, null) };
        Assert.Equal(HttpStatusCode.OK, (await SendWithCsrfAsync(admin, HttpMethod.Post, "/api/work-calendar/bulk", new BulkConfigureWorkCalendarRequest(days, false))).StatusCode);
        const string newPassword = "NeverPersistThis1";
        Assert.Equal(HttpStatusCode.NoContent, (await SendWithCsrfAsync(admin, HttpMethod.Post, $"/api/identity/users/{target!.Id}/reset-password", new ResetIdentityUserPasswordRequest(newPassword))).StatusCode);
        var events = await admin.GetFromJsonAsync<PagedAuditEventsResponse>("/api/audit-events?pageSize=50");
        Assert.Single(events!.Items, x => x.Action == "BulkConfigureWorkCalendar");
        var reset = Assert.Single(events.Items, x => x.Action == "ResetUserPassword");
        Assert.DoesNotContain(newPassword, reset.Metadata!.Value.GetRawText(), StringComparison.Ordinal);
        Assert.Contains("created", events.Items.Single(x => x.Action == "BulkConfigureWorkCalendar").Metadata!.Value.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task CreateUserAsync(UserManager<ApplicationUser> users, string username, string role)
    { var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = username, LockoutEnabled = true }; Assert.True((await users.CreateAsync(user, "Password1")).Succeeded); Assert.True((await users.AddToRoleAsync(user, role)).Succeeded); }
    private static async Task LoginAsync(HttpClient client, string username)
    { var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/auth/csrf"); var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new LoginRequest(username, "Password1")) }; request.Headers.Add("X-CSRF-TOKEN", token!.Token); Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(request)).StatusCode); }
    private static async Task<HttpResponseMessage> SendWithCsrfAsync(HttpClient client, HttpMethod method, string path, object body)
    { var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/auth/csrf"); var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) }; request.Headers.Add("X-CSRF-TOKEN", token!.Token); return await client.SendAsync(request); }
}
