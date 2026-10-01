using Attendance.Api.Modules.Auditing.Application;
using Attendance.Api.Modules.LegacyMigration.Application;
using Attendance.Api.Modules.LegacyMigration.Contracts;
using Microsoft.AspNetCore.Antiforgery;

namespace Attendance.Api.Modules.LegacyMigration.Endpoints;

public static class LegacyProductionImportEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapLegacyProductionImportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/maintenance/legacy-import")
            .WithTags("Maintenance")
            .RequireAuthorization("AdminOnly")
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true));

        group.MapPost(string.Empty, ApplyAsync)
            .Accepts<LegacyProductionImportRequest>("application/json")
            .Produces<LegacyProductionImportResult>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
        return endpoints;
    }

    private static async Task<IResult> ApplyAsync(
        LegacyProductionImportRequest request,
        LegacyProductionImportService service,
        IAuditWriter audit,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            audit.Prepare(context.User, "ImportLegacyAttendance", "LegacyImport", metadata: new
            {
                employeeCount = request.Employees?.Count ?? 0,
                markCount = request.Marks?.Count ?? 0
            });
            return TypedResults.Ok(await service.ApplyAsync(request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return TypedResults.Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
