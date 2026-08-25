using Attendance.Api.Modules.Employees.Application;
using Attendance.Api.Modules.Employees.Contracts;

namespace Attendance.Api.Modules.Employees.Endpoints;

public static class EmployeeEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder endpoints)
    {
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
