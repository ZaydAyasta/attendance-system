using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Identity.Contracts;
using Attendance.Api.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Attendance.Api.Modules.Identity.Application;

public sealed class IdentityUserAdministrationService(
    UserManager<ApplicationUser> userManager,
    AttendanceDbContext dbContext)
{
    public async Task<IReadOnlyList<IdentityUserResponse>> ListAsync()
    {
        var users = await userManager.Users.AsNoTracking().OrderBy(x => x.UserName).ToListAsync();
        var employeeById = await dbContext.Employees.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => new EmployeeSummaryResponse(x.EmployeeCode, $"{x.FirstName} {x.LastName}"));
        var result = new List<IdentityUserResponse>(users.Count);
        foreach (var user in users)
        {
            result.Add(ToResponse(user, (await userManager.GetRolesAsync(user)).SingleOrDefault() ?? string.Empty,
                user.EmployeeId is Guid employeeId && employeeById.TryGetValue(employeeId, out var employee) ? employee : null));
        }
        return result;
    }

    public async Task<(IdentityUserResponse? Value, string? Error)> CreateAsync(CreateIdentityUserRequest request)
    {
        var error = await ValidateAsync(request.Username, request.Email, request.Role, request.EmployeeId);
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
            return (null, ToFriendlyError(create.Errors));
        }
        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            return (null, "No fue posible asignar el rol.");
        }
        return (ToResponse(user, request.Role, await GetEmployeeAsync(user.EmployeeId)), null);
    }

    public async Task<(IdentityUserResponse? Value, string? Error)> UpdateAsync(Guid id, UpdateIdentityUserRequest request)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return (null, "User not found.");
        }
        var error = await ValidateAsync(request.Username, request.Email, request.Role, user.EmployeeId, user.Id);
        if (error is not null)
        {
            return (null, error);
        }

        var oldRoles = await userManager.GetRolesAsync(user);
        if (oldRoles.Contains(IdentityRoles.Admin) && request.Role != IdentityRoles.Admin && await IsLastActiveAdminAsync(user.Id))
            return (null, "No se puede quitar el rol Administrador a la última cuenta Administrador activa.");

        user.UserName = request.Username.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return (null, ToFriendlyError(update.Errors));
        }

        if (!oldRoles.Contains(request.Role))
        {
            var add = await userManager.AddToRoleAsync(user, request.Role);
            if (!add.Succeeded) return (null, "No fue posible actualizar el rol.");
            var obsoleteRoles = oldRoles.Where(x => x != request.Role).ToArray();
            if (obsoleteRoles.Length > 0)
            {
                var remove = await userManager.RemoveFromRolesAsync(user, obsoleteRoles);
                if (!remove.Succeeded) return (null, "No fue posible actualizar el rol.");
            }
        }
        await userManager.UpdateSecurityStampAsync(user);
        return (ToResponse(user, request.Role, await GetEmployeeAsync(user.EmployeeId)), null);
    }

    public async Task<(IdentityUserResponse? Value, string? Error)> SetStatusAsync(Guid id, bool isActive, ClaimsPrincipal actingPrincipal)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return (null, "User not found.");
        var actorId = userManager.GetUserId(actingPrincipal);
        if (!isActive && string.Equals(actorId, user.Id.ToString(), StringComparison.OrdinalIgnoreCase))
            return (null, "No puedes desactivar tu propia cuenta.");

        var roles = await userManager.GetRolesAsync(user);
        if (!isActive && roles.Contains(IdentityRoles.Admin) && await IsLastActiveAdminAsync(user.Id))
            return (null, "No se puede desactivar la última cuenta Administrador activa.");

        user.LockoutEnabled = true;
        user.LockoutEnd = isActive ? null : DateTimeOffset.MaxValue;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) return (null, "No fue posible actualizar el estado de la cuenta.");
        await userManager.UpdateSecurityStampAsync(user);
        return (ToResponse(user, roles.SingleOrDefault() ?? string.Empty, await GetEmployeeAsync(user.EmployeeId)), null);
    }

    public async Task<string?> ResetPasswordAsync(Guid id, string password)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return "User not found.";
        if (string.IsNullOrWhiteSpace(password)) return "La contraseña no cumple los requisitos.";
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, password);
        return result.Succeeded ? null : ToFriendlyError(result.Errors);
    }

    private async Task<string?> ValidateAsync(string username, string? email, string role, Guid? employeeId, Guid? currentUserId = null)
    {
        if (string.IsNullOrWhiteSpace(username)) return "El usuario es obligatorio.";
        if (!IdentityRoles.All.Contains(role)) return "El rol no es válido.";
        if (role == IdentityRoles.User && employeeId is null) return "Una cuenta Usuario requiere un empleado asociado.";
        if (employeeId is Guid id && !await dbContext.Employees.AnyAsync(x => x.Id == id)) return "No se encontró el empleado.";
        if (employeeId is Guid linkedId && await userManager.Users.AnyAsync(x => x.EmployeeId == linkedId && (!currentUserId.HasValue || x.Id != currentUserId.Value)))
            return "Este empleado ya tiene una cuenta.";
        if (!string.IsNullOrWhiteSpace(email))
        {
            var existingEmailUser = await userManager.FindByEmailAsync(email.Trim());
            if (existingEmailUser is not null && existingEmailUser.Id != currentUserId) return "El usuario o correo ya existe.";
        }
        return null;
    }

    private async Task<EmployeeSummaryResponse?> GetEmployeeAsync(Guid? employeeId)
        => employeeId is Guid id ? await dbContext.Employees.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new EmployeeSummaryResponse(x.EmployeeCode, $"{x.FirstName} {x.LastName}")).SingleOrDefaultAsync() : null;

    private async Task<bool> IsLastActiveAdminAsync(Guid userId)
    {
        var adminRoleId = await dbContext.Roles.Where(x => x.Name == IdentityRoles.Admin).Select(x => x.Id).SingleAsync();
        return await dbContext.UserRoles.Where(x => x.RoleId == adminRoleId)
            .Join(dbContext.Users, role => role.UserId, user => user.Id, (_, user) => user)
            .CountAsync(user => user.Id != userId && (user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow)) == 0;
    }

    private static string ToFriendlyError(IEnumerable<IdentityError> errors)
    {
        var codes = errors.Select(x => x.Code).ToArray();
        if (codes.Any(x => x is "DuplicateUserName" or "DuplicateEmail")) return "El usuario o correo ya existe.";
        if (codes.Any(x => x.Contains("Password", StringComparison.OrdinalIgnoreCase))) return "La contraseña no cumple los requisitos.";
        return "No fue posible guardar la cuenta.";
    }

    private static IdentityUserResponse ToResponse(ApplicationUser user, string role, EmployeeSummaryResponse? employee)
        => new(user.Id, user.UserName ?? string.Empty, user.Email, role, user.EmployeeId,
            user.LockoutEnd is null || user.LockoutEnd <= DateTimeOffset.UtcNow, employee);
}
