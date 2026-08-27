using Attendance.Api.Modules.Checkpoints.Application;
using Attendance.Api.Modules.Checkpoints.Contracts;
using Microsoft.AspNetCore.Antiforgery;

namespace Attendance.Api.Modules.Checkpoints.Endpoints;

public static class CheckpointEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCheckpointEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/checkpoints").WithTags("Checkpoints").RequireAuthorization("ITOnly");
        group.MapGet(string.Empty, async (CheckpointService service, CancellationToken ct) => TypedResults.Ok(await service.ListAsync(ct)))
            .WithName("ListCheckpoints").WithSummary("List checkpoints").Produces<IReadOnlyCollection<CheckpointResponse>>();
        group.MapPost(string.Empty, CreateAsync).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
            .WithName("CreateCheckpoint").WithSummary("Create checkpoint").Accepts<CreateCheckpointRequest>("application/json").Produces<CheckpointResponse>(StatusCodes.Status201Created).ProducesValidationProblem();
        group.MapPut("/{id:guid}", UpdateAsync).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
            .WithName("UpdateCheckpoint").WithSummary("Update checkpoint").Accepts<UpdateCheckpointRequest>("application/json").Produces<CheckpointResponse>().Produces(StatusCodes.Status409Conflict);
        group.MapPut("/{id:guid}/status", SetStatusAsync).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
            .WithName("SetCheckpointStatus").WithSummary("Activate or deactivate checkpoint").Accepts<SetCheckpointStatusRequest>("application/json").Produces<CheckpointResponse>().Produces(StatusCodes.Status409Conflict);
        group.MapGet("/{id:guid}/qr", GetQrAsync).WithName("GetCheckpointQr").WithSummary("Generate short-lived checkpoint QR token").Produces<CheckpointQrResponse>().Produces(StatusCodes.Status404NotFound);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(CreateCheckpointRequest request, CheckpointService service, CancellationToken ct)
    {
        try
        {
            var result = await service.CreateAsync(request, ct);
            return result.Status == CheckpointWriteStatus.Success ? TypedResults.Created($"/api/checkpoints/{result.Value!.Id}", result.Value) : WriteProblem(result.Status);
        }
        catch (ArgumentException exception) { return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["checkpoint"] = [exception.Message] }); }
    }

    private static async Task<IResult> UpdateAsync(Guid id, UpdateCheckpointRequest request, CheckpointService service, CancellationToken ct)
    {
        try
        {
            var result = await service.UpdateAsync(id, request, ct);
            return result.Status == CheckpointWriteStatus.Success ? TypedResults.Ok(result.Value) : WriteProblem(result.Status);
        }
        catch (ArgumentException exception) { return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["checkpoint"] = [exception.Message] }); }
    }

    private static async Task<IResult> SetStatusAsync(Guid id, SetCheckpointStatusRequest request, CheckpointService service, CancellationToken ct)
    {
        var result = await service.SetStatusAsync(id, request, ct);
        return result.Status == CheckpointWriteStatus.Success ? TypedResults.Ok(result.Value) : WriteProblem(result.Status);
    }

    private static async Task<IResult> GetQrAsync(Guid id, CheckpointService service, CheckpointQrService qrService, CancellationToken ct)
    {
        var checkpoint = await service.FindAsync(id, ct);
        if (checkpoint is null) return TypedResults.NotFound();
        var payload = qrService.CreatePayload(checkpoint.Id);
        return TypedResults.Ok(new CheckpointQrResponse(qrService.Protect(payload), payload.ExpiresAt));
    }

    private static IResult WriteProblem(CheckpointWriteStatus status) => status switch
    {
        CheckpointWriteStatus.NotFound => TypedResults.NotFound(),
        CheckpointWriteStatus.Duplicate => TypedResults.Conflict(new { title = "Ya existe un checkpoint con ese código." }),
        CheckpointWriteStatus.ConcurrencyConflict => TypedResults.Conflict(new { title = "El checkpoint fue actualizado por otra persona. Recarga e inténtalo otra vez." }),
        _ => TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["checkpoint"] = ["Los datos del checkpoint no son válidos."] })
    };
}
