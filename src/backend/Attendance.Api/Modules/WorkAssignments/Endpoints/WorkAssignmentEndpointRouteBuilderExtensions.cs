using Attendance.Api.Modules.WorkAssignments.Application;
using Attendance.Api.Modules.WorkAssignments.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Attendance.Api.Modules.WorkAssignments.Endpoints;

public static class WorkAssignmentEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapWorkAssignmentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var assignmentsGroup = endpoints
            .MapGroup("/api/work-assignments")
            .WithTags("Work Assignments");

        var employeesGroup = endpoints
            .MapGroup("/api/employees")
            .WithTags("Work Assignments");

        assignmentsGroup.MapGet(string.Empty, ListAsync)
            .WithName("ListWorkAssignments")
            .WithSummary("List work assignments")
            .WithDescription(
                "Returns work assignments filtered by optional employee, date range, status and type criteria.")
            .Produces<IReadOnlyCollection<WorkAssignmentResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        assignmentsGroup.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetWorkAssignmentById")
            .WithSummary("Get a work assignment by id")
            .WithDescription(
                "Returns one work assignment by its identifier.")
            .Produces<WorkAssignmentResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        assignmentsGroup.MapPost(string.Empty, CreateAsync)
            .WithName("CreateWorkAssignment")
            .WithSummary("Create a work assignment")
            .WithDescription(
                "Registers an exceptional work assignment for one employee and date.")
            .Accepts<CreateWorkAssignmentRequest>("application/json")
            .Produces<WorkAssignmentResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        assignmentsGroup.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateWorkAssignment")
            .WithSummary("Update a work assignment")
            .WithDescription(
                "Updates an existing work assignment while preserving optimistic concurrency checks.")
            .Accepts<UpdateWorkAssignmentRequest>("application/json")
            .Produces<WorkAssignmentResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        assignmentsGroup.MapPost("/{id:guid}/cancel", CancelAsync)
            .WithName("CancelWorkAssignment")
            .WithSummary("Cancel a work assignment")
            .WithDescription(
                "Cancels an existing work assignment using optimistic concurrency.")
            .Accepts<CancelWorkAssignmentRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        employeesGroup.MapGet("/{employeeId:guid}/work-assignments", GetEmployeeHistoryAsync)
            .WithName("GetEmployeeWorkAssignments")
            .WithSummary("Get employee work assignments")
            .WithDescription(
                "Returns the work assignment history registered for a specific employee.")
            .Produces<IReadOnlyCollection<WorkAssignmentResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        Guid? employeeId,
        DateOnly? from,
        DateOnly? to,
        string? status,
        string? type,
        WorkAssignmentService service,
        CancellationToken cancellationToken)
    {
        var validation = WorkAssignmentRequestValidator.ValidateList(
            employeeId,
            from,
            to,
            status,
            type);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var assignments = await service.ListAsync(validation.Value!, cancellationToken);

        return TypedResults.Ok(assignments);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        WorkAssignmentService service,
        CancellationToken cancellationToken)
    {
        var validation = WorkAssignmentRequestValidator.ValidateId(id);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var assignment = await service.GetByIdAsync(id, cancellationToken);

        return assignment is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(assignment);
    }

    private static async Task<IResult> GetEmployeeHistoryAsync(
        Guid employeeId,
        WorkAssignmentService service,
        CancellationToken cancellationToken)
    {
        var validation = WorkAssignmentRequestValidator.ValidateEmployeeId(employeeId);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var result = await service.GetEmployeeHistoryAsync(employeeId, cancellationToken);

        return result.Status switch
        {
            WorkAssignmentEmployeeHistoryStatus.Success => TypedResults.Ok(result.Value),
            WorkAssignmentEmployeeHistoryStatus.EmployeeNotFound => TypedResults.NotFound(),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                detail: "An unexpected error occurred while retrieving work assignments.")
        };
    }

    private static async Task<IResult> CreateAsync(
        CreateWorkAssignmentRequest request,
        WorkAssignmentService service,
        CancellationToken cancellationToken)
    {
        var validation = WorkAssignmentRequestValidator.ValidateCreate(request);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var result = await service.CreateAsync(validation.Value!, cancellationToken);

        return result.Status switch
        {
            WorkAssignmentWriteStatus.Success => TypedResults.CreatedAtRoute(
                result.Value,
                "GetWorkAssignmentById",
                new { id = result.Value!.Id }),
            WorkAssignmentWriteStatus.EmployeeNotFound => TypedResults.NotFound(),
            WorkAssignmentWriteStatus.EmployeeInactive => TypedResults.Conflict(
                CreateProblemDetails(
                    "Employee is inactive.",
                    "Work assignments cannot be created for inactive employees.")),
            WorkAssignmentWriteStatus.DuplicateActiveAssignment => TypedResults.Conflict(
                CreateProblemDetails(
                    "Active work assignment conflict.",
                    "The employee already has an active work assignment for the requested date.")),
            WorkAssignmentWriteStatus.HolidayConflict => TypedResults.Conflict(
                CreateProblemDetails(
                    "Holiday assignment conflict.",
                    "Active work assignments cannot be registered on dates classified as holidays.")),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                detail: "An unexpected error occurred while creating the work assignment.")
        };
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateWorkAssignmentRequest request,
        WorkAssignmentService service,
        CancellationToken cancellationToken)
    {
        var idValidation = WorkAssignmentRequestValidator.ValidateId(id);

        if (!idValidation.IsValid)
        {
            return TypedResults.ValidationProblem(idValidation.Errors);
        }

        var validation = WorkAssignmentRequestValidator.ValidateUpdate(request);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var result = await service.UpdateAsync(id, validation.Value!, cancellationToken);

        return result.Status switch
        {
            WorkAssignmentWriteStatus.Success => TypedResults.Ok(result.Value),
            WorkAssignmentWriteStatus.NotFound => TypedResults.NotFound(),
            WorkAssignmentWriteStatus.InvalidState => TypedResults.Conflict(
                CreateProblemDetails(
                    "Work assignment is cancelled.",
                    "Cancelled work assignments are historical records and cannot be modified.")),
            WorkAssignmentWriteStatus.DuplicateActiveAssignment => TypedResults.Conflict(
                CreateProblemDetails(
                    "Active work assignment conflict.",
                    "The employee already has an active work assignment for the requested date.")),
            WorkAssignmentWriteStatus.HolidayConflict => TypedResults.Conflict(
                CreateProblemDetails(
                    "Holiday assignment conflict.",
                    "Active work assignments cannot be registered on dates classified as holidays.")),
            WorkAssignmentWriteStatus.ConcurrencyConflict => TypedResults.Conflict(
                CreateProblemDetails(
                    "Concurrency conflict.",
                    "The work assignment was modified by another operation.")),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                detail: "An unexpected error occurred while updating the work assignment.")
        };
    }

    private static async Task<IResult> CancelAsync(
        Guid id,
        CancelWorkAssignmentRequest request,
        WorkAssignmentService service,
        CancellationToken cancellationToken)
    {
        var idValidation = WorkAssignmentRequestValidator.ValidateId(id);

        if (!idValidation.IsValid)
        {
            return TypedResults.ValidationProblem(idValidation.Errors);
        }

        var validation = WorkAssignmentRequestValidator.ValidateCancel(request);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var result = await service.CancelAsync(id, validation.Value!, cancellationToken);

        return result.Status switch
        {
            WorkAssignmentWriteStatus.Success => TypedResults.NoContent(),
            WorkAssignmentWriteStatus.NotFound => TypedResults.NotFound(),
            WorkAssignmentWriteStatus.ConcurrencyConflict => TypedResults.Conflict(
                CreateProblemDetails(
                    "Concurrency conflict.",
                    "The work assignment was modified by another operation.")),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                detail: "An unexpected error occurred while cancelling the work assignment.")
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
