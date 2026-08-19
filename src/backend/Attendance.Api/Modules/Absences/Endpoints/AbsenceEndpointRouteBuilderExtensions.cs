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

        absencesGroup.MapGet(string.Empty, ListAsync);
        absencesGroup.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetAbsenceById");
        absencesGroup.MapPost(string.Empty, CreateAsync);
        absencesGroup.MapPut("/{id:guid}", UpdateAsync);
        absencesGroup.MapPost("/{id:guid}/cancel", CancelAsync);

        employeesGroup.MapGet("/{employeeId:guid}/absences", GetEmployeeHistoryAsync);

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
