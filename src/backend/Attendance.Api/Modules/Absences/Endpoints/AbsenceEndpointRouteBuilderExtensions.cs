using Attendance.Api.Modules.Absences.Application;
using Attendance.Api.Modules.Absences.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Attendance.Api.Modules.Absences.Endpoints;

public static class AbsenceEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAbsenceEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var absencesGroup = endpoints
            .MapGroup("/api/absences")
            .WithTags("Absences");

        var employeesGroup = endpoints
            .MapGroup("/api/employees")
            .WithTags("Absences");

        absencesGroup.MapGet(string.Empty, ListAsync)
            .WithName("ListAbsences")
            .WithSummary("List absences")
            .WithDescription(
                "Returns absences filtered by optional employee, date range, status and type criteria.")
            .Produces<IReadOnlyCollection<AbsenceResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        absencesGroup.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetAbsenceById")
            .WithSummary("Get an absence by id")
            .WithDescription(
                "Returns a single absence record by its identifier.")
            .Produces<AbsenceResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        absencesGroup.MapPost(string.Empty, CreateAsync)
            .WithName("CreateAbsence")
            .WithSummary("Create an absence")
            .WithDescription(
                "Registers a new absence for an employee.")
            .Accepts<CreateAbsenceRequest>("application/json")
            .Produces<AbsenceResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        absencesGroup.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateAbsence")
            .WithSummary("Update an absence")
            .WithDescription(
                "Updates an existing absence while preserving optimistic concurrency checks.")
            .Accepts<UpdateAbsenceRequest>("application/json")
            .Produces<AbsenceResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        absencesGroup.MapPost("/{id:guid}/cancel", CancelAsync)
            .WithName("CancelAbsence")
            .WithSummary("Cancel an absence")
            .WithDescription(
                "Cancels an existing absence using optimistic concurrency.")
            .Accepts<CancelAbsenceRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        employeesGroup.MapGet("/{employeeId:guid}/absences", GetEmployeeHistoryAsync)
            .WithName("GetEmployeeAbsenceHistory")
            .WithSummary("Get employee absence history")
            .WithDescription(
                "Returns the absence history registered for a specific employee.")
            .Produces<IReadOnlyList<AbsenceResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        Guid? employeeId,
        DateOnly? from,
        DateOnly? to,
        string? status,
        string? type,
        AbsenceService service,
        CancellationToken cancellationToken)
    {
        var validation = AbsenceRequestValidator.ValidateList(
            employeeId,
            from,
            to,
            status,
            type);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var absences = await service.ListAsync(validation.Value!, cancellationToken);

        return TypedResults.Ok(absences);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        AbsenceService service,
        CancellationToken cancellationToken)
    {
        var validation = AbsenceRequestValidator.ValidateId(id);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var absence = await service.GetByIdAsync(id, cancellationToken);

        return absence is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(absence);
    }

    private static async Task<IResult> GetEmployeeHistoryAsync(
        Guid employeeId,
        AbsenceService service,
        CancellationToken cancellationToken)
    {
        var validation = AbsenceRequestValidator.ValidateEmployeeId(employeeId);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var result = await service.GetEmployeeHistoryAsync(
            employeeId,
            cancellationToken);

        return result.Status switch
        {
            AbsenceEmployeeHistoryStatus.Success => TypedResults.Ok(result.Value),
            AbsenceEmployeeHistoryStatus.EmployeeNotFound => TypedResults.NotFound(),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                detail: "An unexpected error occurred while retrieving absence history.")
        };
    }

    private static async Task<IResult> CreateAsync(
        CreateAbsenceRequest request,
        AbsenceService service,
        CancellationToken cancellationToken)
    {
        var validation = AbsenceRequestValidator.ValidateCreate(request);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var result = await service.CreateAsync(validation.Value!, cancellationToken);

        return result.Status switch
        {
            AbsenceWriteStatus.Success => TypedResults.CreatedAtRoute(
                result.Value,
                "GetAbsenceById",
                new { id = result.Value!.Id }),
            AbsenceWriteStatus.EmployeeNotFound => TypedResults.NotFound(),
            AbsenceWriteStatus.EmployeeInactive => TypedResults.Conflict(
                CreateProblemDetails(
                    "Employee is inactive.",
                    "Absences cannot be created for inactive employees.")),
            AbsenceWriteStatus.OverlapConflict => TypedResults.Conflict(
                CreateProblemDetails(
                    "Absence overlap conflict.",
                    "The employee already has an active absence that overlaps the requested range.")),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                detail: "An unexpected error occurred while creating the absence.")
        };
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateAbsenceRequest request,
        AbsenceService service,
        CancellationToken cancellationToken)
    {
        var idValidation = AbsenceRequestValidator.ValidateId(id);

        if (!idValidation.IsValid)
        {
            return TypedResults.ValidationProblem(idValidation.Errors);
        }

        var validation = AbsenceRequestValidator.ValidateUpdate(request);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var result = await service.UpdateAsync(id, validation.Value!, cancellationToken);

        return result.Status switch
        {
            AbsenceWriteStatus.Success => TypedResults.Ok(result.Value),
            AbsenceWriteStatus.NotFound => TypedResults.NotFound(),
            AbsenceWriteStatus.InvalidState => TypedResults.Conflict(
                CreateProblemDetails(
                    "Absence is cancelled.",
                    "Cancelled absences are historical records and cannot be modified.")),
            AbsenceWriteStatus.OverlapConflict => TypedResults.Conflict(
                CreateProblemDetails(
                    "Absence overlap conflict.",
                    "The employee already has an active absence that overlaps the requested range.")),
            AbsenceWriteStatus.ConcurrencyConflict => TypedResults.Conflict(
                CreateProblemDetails(
                    "Concurrency conflict.",
                    "The absence was modified by another operation.")),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                detail: "An unexpected error occurred while updating the absence.")
        };
    }

    private static async Task<IResult> CancelAsync(
        Guid id,
        CancelAbsenceRequest request,
        AbsenceService service,
        CancellationToken cancellationToken)
    {
        var idValidation = AbsenceRequestValidator.ValidateId(id);

        if (!idValidation.IsValid)
        {
            return TypedResults.ValidationProblem(idValidation.Errors);
        }

        var validation = AbsenceRequestValidator.ValidateCancel(request);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var result = await service.CancelAsync(id, validation.Value!, cancellationToken);

        return result.Status switch
        {
            AbsenceWriteStatus.Success => TypedResults.NoContent(),
            AbsenceWriteStatus.NotFound => TypedResults.NotFound(),
            AbsenceWriteStatus.ConcurrencyConflict => TypedResults.Conflict(
                CreateProblemDetails(
                    "Concurrency conflict.",
                    "The absence was modified by another operation.")),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                detail: "An unexpected error occurred while cancelling the absence.")
        };
    }

    private static ProblemDetails CreateProblemDetails(
        string title,
        string detail)
        => new()
        {
            Title = title,
            Detail = detail,
            Status = StatusCodes.Status409Conflict
        };
}
