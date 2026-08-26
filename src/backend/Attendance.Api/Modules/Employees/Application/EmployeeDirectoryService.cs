using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Employees.Contracts;
using Attendance.Api.Modules.Employees.Domain;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Api.Modules.Employees.Application;

public sealed class EmployeeDirectoryService(AttendanceDbContext dbContext)
{
    public Task<List<EmployeeResponse>> ListDetailsAsync(CancellationToken ct)=>dbContext.Employees.AsNoTracking().OrderBy(x=>x.LastName).Select(x=>new EmployeeResponse(x.Id,x.EmployeeCode,x.FirstName,x.LastName,x.IsActive,x.HireDate,x.TerminationDate,x.Version)).ToListAsync(ct);
    public async Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest r,CancellationToken ct){var e=Employee.Create(r.EmployeeCode,r.FirstName,r.LastName,r.HireDate);dbContext.Employees.Add(e);await dbContext.SaveChangesAsync(ct);return new(e.Id,e.EmployeeCode,e.FirstName,e.LastName,e.IsActive,e.HireDate,e.TerminationDate,e.Version);}
    public async Task<EmployeeResponse?> UpdateAsync(Guid id,UpdateEmployeeRequest r,CancellationToken ct){var e=await dbContext.Employees.SingleOrDefaultAsync(x=>x.Id==id,ct);if(e is null)return null; e.Update(r.EmployeeCode,r.FirstName,r.LastName,r.HireDate);dbContext.Entry(e).Property(x=>x.Version).OriginalValue=r.Version;await dbContext.SaveChangesAsync(ct);return new(e.Id,e.EmployeeCode,e.FirstName,e.LastName,e.IsActive,e.HireDate,e.TerminationDate,e.Version);}
    public async Task<EmployeeResponse?> SetStatusAsync(Guid id,SetEmployeeStatusRequest r,CancellationToken ct){var e=await dbContext.Employees.SingleOrDefaultAsync(x=>x.Id==id,ct);if(e is null)return null;e.SetActive(r.IsActive,r.TerminationDate);dbContext.Entry(e).Property(x=>x.Version).OriginalValue=r.Version;await dbContext.SaveChangesAsync(ct);return new(e.Id,e.EmployeeCode,e.FirstName,e.LastName,e.IsActive,e.HireDate,e.TerminationDate,e.Version);}
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
