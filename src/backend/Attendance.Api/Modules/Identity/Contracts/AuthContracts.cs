namespace Attendance.Api.Modules.Identity.Contracts;

public sealed record LoginRequest(string UsernameOrEmail, string Password, bool RememberMe = false);

public sealed record EmployeeSummaryResponse(string EmployeeCode, string FullName);

public sealed record CurrentUserResponse(
    Guid Id,
    string Username,
    string Role,
    Guid? EmployeeId,
    EmployeeSummaryResponse? Employee);

public sealed record AntiforgeryTokenResponse(string Token);

public sealed record CreateIdentityUserRequest(
    string Username,
    string? Email,
    string Password,
    string Role,
    Guid? EmployeeId);

public sealed record UpdateIdentityUserRequest(
    string Username,
    string? Email,
    string Role);

public sealed record SetIdentityUserStatusRequest(bool IsActive);

public sealed record ResetIdentityUserPasswordRequest(string Password);

public sealed record IdentityUserResponse(
    Guid Id,
    string Username,
    string? Email,
    string Role,
    Guid? EmployeeId,
    bool IsActive,
    EmployeeSummaryResponse? Employee);
