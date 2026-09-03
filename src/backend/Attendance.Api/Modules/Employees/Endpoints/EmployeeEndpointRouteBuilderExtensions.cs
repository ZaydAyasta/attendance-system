using Attendance.Api.Modules.Employees.Application;
using Attendance.Api.Modules.Employees.Contracts;
using Attendance.Api.Modules.Auditing.Application;

namespace Attendance.Api.Modules.Employees.Endpoints;

public static class EmployeeEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/employees/manage", async (EmployeeDirectoryService s,CancellationToken ct)=>TypedResults.Ok(await s.ListDetailsAsync(ct))).RequireAuthorization("AdminOnly");
        endpoints.MapPost("/api/employees", CreateAsync).RequireAuthorization("AdminOnly").WithMetadata(new Microsoft.AspNetCore.Antiforgery.RequireAntiforgeryTokenAttribute(true));
        endpoints.MapPut("/api/employees/{id:guid}", UpdateAsync).RequireAuthorization("AdminOnly").WithMetadata(new Microsoft.AspNetCore.Antiforgery.RequireAntiforgeryTokenAttribute(true));
        endpoints.MapPut("/api/employees/{id:guid}/status", SetStatusAsync).RequireAuthorization("AdminOnly").WithMetadata(new Microsoft.AspNetCore.Antiforgery.RequireAntiforgeryTokenAttribute(true));
        endpoints.MapGet("/api/employees", ListAsync)
            .WithTags("Employees")
            .WithName("ListEmployees")
            .WithSummary("List employees")
            .WithDescription("Returns employees for read-only selection, optionally filtered by active status.")
            .Produces<IReadOnlyCollection<EmployeeOptionResponse>>(StatusCodes.Status200OK)
            .RequireAuthorization("AdminOnly");
        return endpoints;
    }

    private static async Task<IResult> ListAsync(bool? isActive, EmployeeDirectoryService service, CancellationToken cancellationToken)
        => TypedResults.Ok(await service.ListAsync(isActive, cancellationToken));

    private static async Task<IResult> CreateAsync(CreateEmployeeRequest request, EmployeeDirectoryService service, IAuditWriter audit, HttpContext context, CancellationToken cancellationToken)
    {
        audit.Prepare(context.User, "CreateEmployee", "Employee", metadata: new { request.EmployeeCode });
        return TypedResults.Ok(await service.CreateAsync(request, cancellationToken));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateEmployeeRequest request,
        EmployeeDirectoryService service,
        IAuditWriter audit,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        audit.Prepare(context.User, "UpdateEmployee", "Employee", id.ToString(), new { request.EmployeeCode });
        var employee = await service.UpdateAsync(id, request, cancellationToken);
        return employee is null ? TypedResults.NotFound() : TypedResults.Ok(employee);
    }

    private static async Task<IResult> SetStatusAsync(
        Guid id,
        SetEmployeeStatusRequest request,
        EmployeeDirectoryService service,
        IAuditWriter audit,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        audit.Prepare(context.User, request.IsActive ? "ActivateEmployee" : "DeactivateEmployee", "Employee", id.ToString());
        var employee = await service.SetStatusAsync(id, request, cancellationToken);
        return employee is null ? TypedResults.NotFound() : TypedResults.Ok(employee);
    }
}
