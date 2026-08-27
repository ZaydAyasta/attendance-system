using Microsoft.AspNetCore.Identity;

namespace Attendance.Api.Modules.Identity.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? EmployeeId { get; set; }
}
