using Attendance.Api.Modules.Employees.Application;
using Attendance.Api.Modules.Employees.Contracts;

namespace Attendance.Api.Modules.Employees.Endpoints;

public static class EmployeeEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/employees/manage", async (EmployeeDirectoryService s,CancellationToken ct)=>TypedResults.Ok(await s.ListDetailsAsync(ct)));
        endpoints.MapPost("/api/employees", async (CreateEmployeeRequest r,EmployeeDirectoryService s,CancellationToken ct)=>TypedResults.Ok(await s.CreateAsync(r,ct)));
        endpoints.MapPut("/api/employees/{id:guid}", async (Guid id,UpdateEmployeeRequest r,EmployeeDirectoryService s,CancellationToken ct)=>{var x=await s.UpdateAsync(id,r,ct);return x is null?TypedResults.NotFound():TypedResults.Ok(x);});
        endpoints.MapPut("/api/employees/{id:guid}/status", async (Guid id,SetEmployeeStatusRequest r,EmployeeDirectoryService s,CancellationToken ct)=>{var x=await s.SetStatusAsync(id,r,ct);return x is null?TypedResults.NotFound():TypedResults.Ok(x);});
        endpoints.MapGet("/api/employees", ListAsync)
            .WithTags("Employees")
            .WithName("ListEmployees")
            .WithSummary("List employees")
            .WithDescription("Returns employees for read-only selection, optionally filtered by active status.")
            .Produces<IReadOnlyCollection<EmployeeOptionResponse>>(StatusCodes.Status200OK);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(bool? isActive, EmployeeDirectoryService service, CancellationToken cancellationToken)
        => TypedResults.Ok(await service.ListAsync(isActive, cancellationToken));
}
