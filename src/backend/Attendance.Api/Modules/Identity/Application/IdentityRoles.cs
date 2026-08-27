namespace Attendance.Api.Modules.Identity.Application;

public static class IdentityRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string IT = "IT";

    public static readonly IReadOnlyList<string> All = [Admin, User, IT];
}
