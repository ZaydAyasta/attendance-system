using Attendance.Api.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Api.Modules.Attendance.Application;

internal static class AttendanceEmployeeLock
{
    public static Task AcquireAsync(AttendanceDbContext database, Guid employeeId, CancellationToken cancellationToken)
        => database.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT id FROM employees WHERE id = {employeeId} FOR NO KEY UPDATE", cancellationToken);
}
