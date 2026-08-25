using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Employees.Contracts;
using Attendance.Api.Modules.Employees.Domain;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Api.Modules.Employees.Application;

public sealed class EmployeeDirectoryService(AttendanceDbContext dbContext)
{
    public async Task<IReadOnlyList<EmployeeOptionResponse>> ListAsync(
        bool? isActive,
        CancellationToken cancellationToken)
    {
        IQueryable<Employee> query = dbContext.Employees.AsNoTracking();
        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        return await query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Select(x => new EmployeeOptionResponse(
                x.Id,
                x.EmployeeCode,
                $"{x.FirstName} {x.LastName}"))
            .ToListAsync(cancellationToken);
    }
}
