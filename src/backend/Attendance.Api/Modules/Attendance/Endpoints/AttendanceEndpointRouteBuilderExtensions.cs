using Attendance.Api.Modules.Attendance.Application;
using Attendance.Api.Modules.Attendance.Contracts;

namespace Attendance.Api.Modules.Attendance.Endpoints;

public static class AttendanceEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAttendanceEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var employeesGroup = endpoints
            .MapGroup("/api/employees")
            .WithTags("Attendance");

        employeesGroup.MapGet(
            "/{employeeId:guid}/attendance/{date}",
            GetByDateAsync)
            .WithName("GetEmployeeAttendanceByDate")
            .WithSummary("Get daily attendance")
            .WithDescription(
                "Returns the daily attendance evaluation for a specific employee and date.")
            .Produces<DailyAttendanceResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        employeesGroup.MapGet(
            "/{employeeId:guid}/attendance",
            GetRangeAsync)
            .WithName("GetEmployeeAttendanceRange")
            .WithSummary("Get attendance by date range")
            .WithDescription(
                "Returns one evaluated attendance result per day for the requested inclusive range.")
            .Produces<EmployeeAttendanceRangeResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> GetByDateAsync(
        Guid employeeId,
        DateOnly date,
        DailyAttendanceService service,
        CancellationToken cancellationToken)
    {
        var employeeValidation =
            AttendanceRequestValidator.ValidateEmployeeId(employeeId);

        if (!employeeValidation.IsValid)
        {
            return TypedResults.ValidationProblem(employeeValidation.Errors);
        }

        var dateValidation = AttendanceRequestValidator.ValidateDate(date);

        if (!dateValidation.IsValid)
        {
            return TypedResults.ValidationProblem(dateValidation.Errors);
        }

        var result = await service.GetByDateAsync(
            employeeId,
            dateValidation.Value!,
            cancellationToken);

        return result.Status switch
        {
            AttendanceQueryStatus.Success => TypedResults.Ok(result.Value),
            AttendanceQueryStatus.EmployeeNotFound => TypedResults.NotFound(),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                detail: "An unexpected error occurred while retrieving daily attendance.")
        };
    }

    private static async Task<IResult> GetRangeAsync(
        Guid employeeId,
        DateOnly? from,
        DateOnly? to,
        DailyAttendanceService service,
        CancellationToken cancellationToken)
    {
        var employeeValidation =
            AttendanceRequestValidator.ValidateEmployeeId(employeeId);

        if (!employeeValidation.IsValid)
        {
            return TypedResults.ValidationProblem(employeeValidation.Errors);
        }

        var rangeValidation = AttendanceRequestValidator.ValidateRange(from, to);

        if (!rangeValidation.IsValid)
        {
            return TypedResults.ValidationProblem(rangeValidation.Errors);
        }

        var result = await service.GetRangeAsync(
            employeeId,
            rangeValidation.Value!,
            cancellationToken);

        return result.Status switch
        {
            AttendanceQueryStatus.Success => TypedResults.Ok(result.Value),
            AttendanceQueryStatus.EmployeeNotFound => TypedResults.NotFound(),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                detail: "An unexpected error occurred while retrieving attendance range.")
        };
    }
}
