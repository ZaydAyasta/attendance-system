using Attendance.Api.Modules.WorkCalendar.Application;
using Attendance.Api.Modules.WorkCalendar.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Attendance.Api.Modules.WorkCalendar.Endpoints;

public static class WorkCalendarEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapWorkCalendarEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/work-calendar")
            .WithTags("Work Calendar");

        group.MapGet(string.Empty, ListAsync)
            .WithName("ListWorkCalendarDays")
            .WithSummary("List work calendar days")
            .WithDescription(
                "Returns work calendar entries filtered by an optional inclusive date range.")
            .Produces<IReadOnlyCollection<WorkCalendarDayResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{date}", GetByDateAsync)
            .WithName("GetWorkCalendarDayByDate")
            .WithSummary("Get a work calendar day")
            .WithDescription(
                "Returns the labor classification configured for a specific calendar date.")
            .Produces<WorkCalendarDayResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapPost(string.Empty, CreateAsync)
            .WithName("CreateWorkCalendarDay")
            .WithSummary("Create a work calendar day")
            .WithDescription(
                "Creates a work calendar entry for a specific date.")
            .Accepts<CreateWorkCalendarDayRequest>("application/json")
            .Produces<WorkCalendarDayResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{date}", UpdateAsync)
            .WithName("UpdateWorkCalendarDay")
            .WithSummary("Update a work calendar day")
            .WithDescription(
                "Updates the labor classification or description of an existing work calendar entry.")
            .Accepts<UpdateWorkCalendarDayRequest>("application/json")
            .Produces<WorkCalendarDayResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapDelete("/{date}", DeleteAsync)
            .WithName("DeleteWorkCalendarDay")
            .WithSummary("Delete a work calendar day")
            .WithDescription(
                "Deletes an existing work calendar entry for the specified date.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        DateOnly? from,
        DateOnly? to,
        WorkCalendarService service,
        CancellationToken cancellationToken)
    {
        var validation = WorkCalendarRequestValidator.ValidateRange(from, to);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var days = await service.ListAsync(from, to, cancellationToken);

        return TypedResults.Ok(days);
    }

    private static async Task<IResult> GetByDateAsync(
        DateOnly date,
        WorkCalendarService service,
        CancellationToken cancellationToken)
    {
        var validation = WorkCalendarRequestValidator.ValidateDate(date);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var day = await service.GetByDateAsync(date, cancellationToken);

        return day is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(day);
    }

    private static async Task<IResult> CreateAsync(
        CreateWorkCalendarDayRequest request,
        WorkCalendarService service,
        CancellationToken cancellationToken)
    {
        var validation = WorkCalendarRequestValidator.ValidateCreate(request);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var result = await service.CreateAsync(
            validation.Value!,
            cancellationToken);

        return result.Status switch
        {
            WorkCalendarWriteStatus.Success => TypedResults.CreatedAtRoute(
                result.Value,
                "GetWorkCalendarDayByDate",
                new { date = result.Value!.Date }),
            WorkCalendarWriteStatus.Duplicate => TypedResults.Conflict(
                CreateProblemDetails(
                    "Work calendar day already exists.",
                    $"A work calendar day for '{request.Date:yyyy-MM-dd}' already exists.")),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                detail: "An unexpected error occurred while creating the work calendar day.")
        };
    }

    private static async Task<IResult> UpdateAsync(
        DateOnly date,
        UpdateWorkCalendarDayRequest request,
        WorkCalendarService service,
        CancellationToken cancellationToken)
    {
        var dateValidation = WorkCalendarRequestValidator.ValidateDate(date);

        if (!dateValidation.IsValid)
        {
            return TypedResults.ValidationProblem(dateValidation.Errors);
        }

        var validation = WorkCalendarRequestValidator.ValidateUpdate(request);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var result = await service.UpdateAsync(
            date,
            validation.Value!,
            cancellationToken);

        return result.Status switch
        {
            WorkCalendarWriteStatus.Success => TypedResults.Ok(result.Value),
            WorkCalendarWriteStatus.NotFound => TypedResults.NotFound(),
            WorkCalendarWriteStatus.ConcurrencyConflict => TypedResults.Conflict(
                CreateProblemDetails(
                    "Concurrency conflict.",
                    "The work calendar day was modified or removed by another operation.")),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                detail: "An unexpected error occurred while updating the work calendar day.")
        };
    }

    private static async Task<IResult> DeleteAsync(
        DateOnly date,
        WorkCalendarService service,
        CancellationToken cancellationToken)
    {
        var validation = WorkCalendarRequestValidator.ValidateDate(date);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        var result = await service.DeleteAsync(date, cancellationToken);

        return result.Status switch
        {
            WorkCalendarWriteStatus.Success => TypedResults.NoContent(),
            WorkCalendarWriteStatus.NotFound => TypedResults.NotFound(),
            WorkCalendarWriteStatus.ConcurrencyConflict => TypedResults.Conflict(
                CreateProblemDetails(
                    "Concurrency conflict.",
                    "The work calendar day was modified or removed by another operation.")),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                detail: "An unexpected error occurred while deleting the work calendar day.")
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
