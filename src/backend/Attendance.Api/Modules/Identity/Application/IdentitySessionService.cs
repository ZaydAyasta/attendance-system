using System.Security.Claims;
using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Identity.Contracts;
using Attendance.Api.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Api.Modules.Identity.Application;

public sealed class IdentitySessionService(
    UserManager<ApplicationUser> userManager,
    AttendanceDbContext dbContext)
{
    public async Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal principal)
        => await userManager.GetUserAsync(principal);

    public async Task<CurrentUserResponse?> GetCurrentUserResponseAsync(ClaimsPrincipal principal)
    {
        var user = await GetCurrentUserAsync(principal);
        if (user is null)
        {
            return null;
        }

        return await GetResponseAsync(user);
    }

    public async Task<CurrentUserResponse> GetResponseAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var role = roles.SingleOrDefault() ?? string.Empty;
        EmployeeSummaryResponse? employee = null;

        if (user.EmployeeId is Guid employeeId)
        {
            employee = await dbContext.Employees.AsNoTracking()
                .Where(x => x.Id == employeeId)
                .Select(x => new EmployeeSummaryResponse(
                    x.EmployeeCode,
                    $"{x.FirstName} {x.LastName}"))
                .SingleOrDefaultAsync();
        }

        return new CurrentUserResponse(user.Id, user.UserName ?? string.Empty, role, user.EmployeeId, employee);
    }

    public async Task<Guid?> GetEmployeeIdForCurrentUserAsync(ClaimsPrincipal principal)
    {
        var user = await GetCurrentUserAsync(principal);
        return user?.EmployeeId;
    }
}
