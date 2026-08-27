using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Identity.Contracts;
using Attendance.Api.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Api.Modules.Identity.Application;

public sealed class IdentityUserAdministrationService(
    UserManager<ApplicationUser> userManager,
    AttendanceDbContext dbContext)
{
    public async Task<IReadOnlyList<IdentityUserResponse>> ListAsync()
    {
        var users = await userManager.Users.AsNoTracking().OrderBy(x => x.UserName).ToListAsync();
        var result = new List<IdentityUserResponse>(users.Count);
        foreach (var user in users)
        {
            result.Add(ToResponse(user, (await userManager.GetRolesAsync(user)).SingleOrDefault() ?? string.Empty));
        }
        return result;
    }

    public async Task<(IdentityUserResponse? Value, string? Error)> CreateAsync(CreateIdentityUserRequest request)
    {
        var error = await ValidateAsync(request.Username, request.Role, request.EmployeeId);
        if (error is not null)
        {
            return (null, error);
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Username.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            EmployeeId = request.EmployeeId,
            LockoutEnabled = true,
        };
        var create = await userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            return (null, string.Join(" ", create.Errors.Select(x => x.Description)));
        }
        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            return (null, string.Join(" ", roleResult.Errors.Select(x => x.Description)));
        }
        return (ToResponse(user, request.Role), null);
    }

    public async Task<(IdentityUserResponse? Value, string? Error)> UpdateAsync(Guid id, UpdateIdentityUserRequest request)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return (null, "User not found.");
        }
        var error = await ValidateAsync(request.Username, request.Role, request.EmployeeId);
        if (error is not null)
        {
            return (null, error);
        }

        user.UserName = request.Username.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        user.EmployeeId = request.EmployeeId;
        user.LockoutEnabled = true;
        user.LockoutEnd = request.IsActive ? null : DateTimeOffset.MaxValue;
        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return (null, string.Join(" ", update.Errors.Select(x => x.Description)));
        }

        var oldRoles = await userManager.GetRolesAsync(user);
        var remove = await userManager.RemoveFromRolesAsync(user, oldRoles);
        if (!remove.Succeeded)
        {
            return (null, string.Join(" ", remove.Errors.Select(x => x.Description)));
        }
        var add = await userManager.AddToRoleAsync(user, request.Role);
        if (!add.Succeeded)
        {
            return (null, string.Join(" ", add.Errors.Select(x => x.Description)));
        }
        return (ToResponse(user, request.Role), null);
    }

    private async Task<string?> ValidateAsync(string username, string role, Guid? employeeId)
    {
        if (string.IsNullOrWhiteSpace(username)) return "Username is required.";
        if (!IdentityRoles.All.Contains(role)) return "Role is invalid.";
        if (role == IdentityRoles.User && employeeId is null) return "A User account requires an employee association.";
        if (employeeId is Guid id && !await dbContext.Employees.AnyAsync(x => x.Id == id)) return "Employee not found.";
        return null;
    }

    private static IdentityUserResponse ToResponse(ApplicationUser user, string role)
        => new(user.Id, user.UserName ?? string.Empty, user.Email, role, user.EmployeeId,
            user.LockoutEnd is null || user.LockoutEnd <= DateTimeOffset.UtcNow);
}
