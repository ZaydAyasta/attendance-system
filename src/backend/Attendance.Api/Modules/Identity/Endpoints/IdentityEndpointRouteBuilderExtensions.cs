using Attendance.Api.Modules.Absences.Application;
using Attendance.Api.Modules.Attendance.Application;
using Attendance.Api.Modules.Identity.Application;
using Attendance.Api.Modules.Identity.Contracts;
using Attendance.Api.Modules.Identity.Domain;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Attendance.Api.Modules.Identity.Endpoints;

public static class IdentityEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/auth").WithTags("Authentication");
        auth.MapGet("/csrf", GetCsrfTokenAsync).AllowAnonymous()
            .WithName("GetAntiforgeryToken").WithSummary("Get antiforgery token");
        auth.MapPost("/login", LoginAsync).AllowAnonymous().WithMetadata(new RequireAntiforgeryTokenAttribute(true))
            .WithName("Login").WithSummary("Sign in with username or email")
            .Accepts<LoginRequest>("application/json").Produces<CurrentUserResponse>()
            .Produces(StatusCodes.Status401Unauthorized);
        auth.MapPost("/logout", LogoutAsync).RequireAuthorization().WithMetadata(new RequireAntiforgeryTokenAttribute(true))
            .WithName("Logout").WithSummary("Sign out current session").Produces(StatusCodes.Status204NoContent);

        endpoints.MapGet("/api/me", GetMeAsync).RequireAuthorization()
            .WithTags("Authentication").WithName("GetCurrentUser").WithSummary("Get current session")
            .Produces<CurrentUserResponse>().Produces(StatusCodes.Status401Unauthorized);

        var me = endpoints.MapGroup("/api/me").WithTags("My data").RequireAuthorization("EmployeeSelfService");
        me.MapGet("/attendance", GetMyAttendanceRangeAsync)
            .WithName("GetMyAttendanceRange").WithSummary("Get current user's attendance")
            .Produces(StatusCodes.Status403Forbidden);
        me.MapGet("/attendance/{date}", GetMyAttendanceByDateAsync)
            .WithName("GetMyAttendanceByDate").WithSummary("Get current user's daily attendance")
            .Produces(StatusCodes.Status403Forbidden);
        me.MapGet("/absences", GetMyAbsencesAsync)
            .WithName("GetMyAbsences").WithSummary("Get current user's absences")
            .Produces(StatusCodes.Status403Forbidden);

        var administration = endpoints.MapGroup("/api/identity/users")
            .WithTags("Identity users").RequireAuthorization("AdminOnly").WithMetadata(new RequireAntiforgeryTokenAttribute(true));
        administration.MapGet(string.Empty, ListUsersAsync).WithName("ListIdentityUsers").WithSummary("List application users");
        administration.MapPost(string.Empty, CreateUserAsync).WithName("CreateIdentityUser")
            .Accepts<CreateIdentityUserRequest>("application/json").Produces<IdentityUserResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);
        administration.MapPut("/{id:guid}", UpdateUserAsync).WithName("UpdateIdentityUser")
            .Accepts<UpdateIdentityUserRequest>("application/json").Produces<IdentityUserResponse>()
            .Produces(StatusCodes.Status404NotFound).ProducesValidationProblem(StatusCodes.Status400BadRequest);
        administration.MapPut("/{id:guid}/status", SetStatusAsync).WithName("SetIdentityUserStatus")
            .Accepts<SetIdentityUserStatusRequest>("application/json").Produces<IdentityUserResponse>()
            .Produces(StatusCodes.Status404NotFound).ProducesValidationProblem(StatusCodes.Status400BadRequest);
        administration.MapPost("/{id:guid}/reset-password", ResetPasswordAsync).WithName("ResetIdentityUserPassword")
            .Accepts<ResetIdentityUserPasswordRequest>("application/json").Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound).ProducesValidationProblem(StatusCodes.Status400BadRequest);
        return endpoints;
    }

    private static AntiforgeryTokenResponse GetCsrfTokenAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        return new AntiforgeryTokenResponse(tokens.RequestToken ?? string.Empty);
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IdentitySessionService sessions)
    {
        if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
            return TypedResults.Unauthorized();
        var value = request.UsernameOrEmail.Trim();
        var user = await userManager.FindByNameAsync(value) ?? await userManager.FindByEmailAsync(value);
        if (user is null) return TypedResults.Unauthorized();
        if (await userManager.IsLockedOutAsync(user)) return TypedResults.Unauthorized();
        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded) return TypedResults.Unauthorized();
        await signInManager.SignInAsync(user, isPersistent: request.RememberMe);
        return TypedResults.Ok(await sessions.GetResponseAsync(user));
    }

    private static async Task<IResult> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetMeAsync(HttpContext context, IdentitySessionService sessions)
    {
        var response = await sessions.GetCurrentUserResponseAsync(context.User);
        return response is null ? TypedResults.Unauthorized() : TypedResults.Ok(response);
    }

    private static async Task<IResult> GetMyAttendanceRangeAsync(DateOnly? from, DateOnly? to, HttpContext context, IdentitySessionService sessions, DailyAttendanceService attendance, CancellationToken cancellationToken)
    {
        var employeeId = await sessions.GetEmployeeIdForCurrentUserAsync(context.User);
        if (employeeId is null) return MissingEmployee();
        var validation = AttendanceRequestValidator.ValidateRange(from, to);
        if (!validation.IsValid) return TypedResults.ValidationProblem(validation.Errors);
        var result = await attendance.GetRangeAsync(employeeId.Value, validation.Value!, cancellationToken);
        return result.Status == AttendanceQueryStatus.Success ? TypedResults.Ok(result.Value) : TypedResults.NotFound();
    }

    private static async Task<IResult> GetMyAttendanceByDateAsync(DateOnly date, HttpContext context, IdentitySessionService sessions, DailyAttendanceService attendance, CancellationToken cancellationToken)
    {
        var employeeId = await sessions.GetEmployeeIdForCurrentUserAsync(context.User);
        if (employeeId is null) return MissingEmployee();
        var validation = AttendanceRequestValidator.ValidateDate(date);
        if (!validation.IsValid) return TypedResults.ValidationProblem(validation.Errors);
        var result = await attendance.GetByDateAsync(employeeId.Value, validation.Value!, cancellationToken);
        return result.Status == AttendanceQueryStatus.Success ? TypedResults.Ok(result.Value) : TypedResults.NotFound();
    }

    private static async Task<IResult> GetMyAbsencesAsync(DateOnly? from, DateOnly? to, string? status, string? type, HttpContext context, IdentitySessionService sessions, AbsenceService absences, CancellationToken cancellationToken)
    {
        var employeeId = await sessions.GetEmployeeIdForCurrentUserAsync(context.User);
        if (employeeId is null) return MissingEmployee();
        var validation = AbsenceRequestValidator.ValidateList(employeeId, from, to, status, type);
        if (!validation.IsValid) return TypedResults.ValidationProblem(validation.Errors);
        return TypedResults.Ok(await absences.ListAsync(validation.Value!, cancellationToken));
    }

    private static async Task<IResult> ListUsersAsync(IdentityUserAdministrationService service)
        => TypedResults.Ok(await service.ListAsync());

    private static async Task<IResult> CreateUserAsync(CreateIdentityUserRequest request, IdentityUserAdministrationService service)
    {
        var result = await service.CreateAsync(request);
        return result.Value is null ? TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["identity"] = [result.Error ?? "Invalid user."] }) : TypedResults.Created($"/api/identity/users/{result.Value.Id}", result.Value);
    }

    private static async Task<IResult> UpdateUserAsync(Guid id, UpdateIdentityUserRequest request, IdentityUserAdministrationService service)
    {
        var result = await service.UpdateAsync(id, request);
        if (result.Value is not null) return TypedResults.Ok(result.Value);
        return result.Error == "User not found." ? TypedResults.NotFound() : TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["identity"] = [result.Error ?? "Invalid user."] });
    }

    private static async Task<IResult> SetStatusAsync(Guid id, SetIdentityUserStatusRequest request, HttpContext context, IdentityUserAdministrationService service)
    {
        var result = await service.SetStatusAsync(id, request.IsActive, context.User);
        if (result.Value is not null) return TypedResults.Ok(result.Value);
        return result.Error == "User not found." ? TypedResults.NotFound() : TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["identity"] = [result.Error ?? "Invalid user."] });
    }

    private static async Task<IResult> ResetPasswordAsync(Guid id, ResetIdentityUserPasswordRequest request, IdentityUserAdministrationService service)
    {
        var error = await service.ResetPasswordAsync(id, request.Password);
        return error is null ? TypedResults.NoContent() : error == "User not found."
            ? TypedResults.NotFound()
            : TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["identity"] = [error] });
    }

    private static IResult MissingEmployee() => TypedResults.Problem("The current User account is not associated with an employee.", statusCode: StatusCodes.Status403Forbidden, title: "Employee association required.");
}
