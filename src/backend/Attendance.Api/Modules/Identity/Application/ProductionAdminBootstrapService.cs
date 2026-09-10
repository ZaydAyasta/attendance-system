using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Attendance.Api.Modules.Identity.Application;

/// <summary>
/// Creates the first production administrator only when an operator explicitly
/// enables the bootstrap through secret configuration. It is not a public API
/// and it never supplies a default credential.
/// </summary>
public sealed class ProductionAdminBootstrapService(
    UserManager<ApplicationUser> userManager,
    AttendanceDbContext dbContext)
{
    public async Task<bool> BootstrapAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled(configuration)) return false;

        var administrators = await userManager.GetUsersInRoleAsync(IdentityRoles.Admin);
        if (administrators.Any(IsActive)) return false;

        var username = configuration["Identity:BootstrapAdmin:Username"]?.Trim();
        var password = configuration["Identity:BootstrapAdmin:Password"];
        var email = OptionalEmail.Normalize(configuration["Identity:BootstrapAdmin:Email"]);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Identity bootstrap is enabled but the administrator username or password is missing.");
        }
        if (!OptionalEmail.IsValid(email))
        {
            throw new InvalidOperationException("Identity bootstrap email is invalid.");
        }

        if (await userManager.FindByNameAsync(username) is not null)
        {
            throw new InvalidOperationException(
                "Identity bootstrap cannot reuse an existing account. Reactivate or repair the existing administrator instead.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            Email = email,
            LockoutEnabled = true
        };
        var create = await userManager.CreateAsync(user, password);
        if (!create.Succeeded)
        {
            throw new InvalidOperationException("Identity bootstrap could not create the administrator account.");
        }

        var addRole = await userManager.AddToRoleAsync(user, IdentityRoles.Admin);
        if (!addRole.Succeeded)
        {
            throw new InvalidOperationException("Identity bootstrap could not assign the administrator role.");
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static bool IsEnabled(IConfiguration configuration)
    {
        var configuredValue = configuration["Identity:BootstrapAdmin:Enabled"];
        if (string.IsNullOrWhiteSpace(configuredValue)) return false;
        if (!bool.TryParse(configuredValue, out var enabled))
        {
            throw new InvalidOperationException("Identity:BootstrapAdmin:Enabled must be true or false.");
        }

        return enabled;
    }

    private static bool IsActive(ApplicationUser user)
        => user.LockoutEnd is null || user.LockoutEnd <= DateTimeOffset.UtcNow;
}
