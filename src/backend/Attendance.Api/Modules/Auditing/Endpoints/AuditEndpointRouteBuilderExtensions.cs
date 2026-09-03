using Attendance.Api.Modules.Auditing.Application;
using Attendance.Api.Modules.Auditing.Contracts;

namespace Attendance.Api.Modules.Auditing.Endpoints;

public static class AuditEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/audit-events", ListAsync)
            .WithTags("Auditing")
            .WithName("ListAuditEvents")
            .WithSummary("List administrative audit events")
            .WithDescription("Returns administrative events in descending UTC occurrence order. Results are paginated and visible only to administrators.")
            .Produces<PagedAuditEventsResponse>()
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("AdminOnly");
        return endpoints;
    }

    private static async Task<IResult> ListAsync(DateOnly? from, DateOnly? to, Guid? actorUserId, string? action, string? entityType, int? page, int? pageSize, AuditQueryService service, CancellationToken cancellationToken)
    {
        if (from is not null && to is not null && from > to)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["dateRange"] = ["La fecha desde no puede ser posterior a la fecha hasta."] });
        var normalizedPage = Math.Max(page ?? 1, 1);
        var normalizedPageSize = Math.Clamp(pageSize ?? 50, 1, 200);
        return TypedResults.Ok(await service.ListAsync(from, to, actorUserId, action, entityType, normalizedPage, normalizedPageSize, cancellationToken));
    }
}
