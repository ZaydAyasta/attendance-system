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

        group.MapGet(string.Empty, ListAsync);
        group.MapGet("/{date}", GetByDateAsync)
            .WithName("GetWorkCalendarDayByDate");
        group.MapPost(string.Empty, CreateAsync);
        group.MapPut("/{date}", UpdateAsync);
        group.MapDelete("/{date}", DeleteAsync);

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
