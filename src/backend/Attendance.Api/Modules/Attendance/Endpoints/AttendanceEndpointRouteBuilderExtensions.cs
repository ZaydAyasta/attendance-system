using Attendance.Api.Modules.Attendance.Application;
using Attendance.Api.Modules.Attendance.Contracts;
using Attendance.Api.Modules.Identity.Application;
using Microsoft.AspNetCore.Antiforgery;

namespace Attendance.Api.Modules.Attendance.Endpoints;

public static class AttendanceEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAttendanceEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var employeesGroup = endpoints
            .MapGroup("/api/employees")
            .WithTags("Attendance")
            .RequireAuthorization("AdminOnly");

        employeesGroup.MapGet(
            "/{employeeId:guid}/attendance/{date}",
            GetByDateAsync)
            .WithName("GetEmployeeAttendanceByDate")
            .WithSummary("Get daily attendance")
            .WithDescription(
                "Returns the daily attendance evaluation and observed worked-time calculation for a specific employee and date.")
            .Produces<DailyAttendanceResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        var captureGroup = endpoints.MapGroup("/api/attendance/capture")
            .WithTags("Attendance capture").RequireAuthorization("EmployeeSelfService");
        captureGroup.MapPost("/resolve", ResolveCaptureAsync).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
            .WithName("ResolveAttendanceCapture").WithSummary("Resolve valid actions for a dynamic checkpoint QR")
            .Accepts<ResolveAttendanceCaptureRequest>("application/json").Produces<AttendanceCaptureResolutionResponse>().Produces(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status403Forbidden);
        captureGroup.MapPost("/mark", MarkCaptureAsync).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
            .WithName("CreateAttendanceCaptureMark").WithSummary("Register an attendance mark from a dynamic checkpoint QR")
            .Accepts<CreateAttendanceCaptureMarkRequest>("application/json").Produces<AttendanceCaptureMarkResponse>().Produces(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status409Conflict).Produces(StatusCodes.Status403Forbidden);

        endpoints.MapGet("/api/me/attendance/marks/today", GetMyMarksTodayAsync).WithTags("My data")
            .RequireAuthorization("EmployeeSelfService").WithName("GetMyAttendanceMarksToday").WithSummary("Get current user's marks for the local day")
            .Produces<IReadOnlyCollection<MyAttendanceMarkResponse>>().Produces(StatusCodes.Status403Forbidden);

        employeesGroup.MapGet(
            "/{employeeId:guid}/attendance",
            GetRangeAsync)
            .WithName("GetEmployeeAttendanceRange")
            .WithSummary("Get attendance by date range")
            .WithDescription(
                "Returns one daily attendance evaluation and worked-time calculation per day for the requested inclusive range.")
            .Produces<EmployeeAttendanceRangeResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> ResolveCaptureAsync(ResolveAttendanceCaptureRequest request, HttpContext context, IdentitySessionService sessions, AttendanceCaptureService service, CancellationToken ct)
    {
        var employeeId = await sessions.GetEmployeeIdForCurrentUserAsync(context.User);
        if (employeeId is null) return MissingEmployee();
        var result = await service.ResolveAsync(employeeId.Value, request.QrToken, ct);
        return result.Status == AttendanceCaptureStatus.Success ? TypedResults.Ok(result.Value!) : CaptureProblem(result.Status, result.Message);
    }

    private static async Task<IResult> MarkCaptureAsync(CreateAttendanceCaptureMarkRequest request, HttpContext context, IdentitySessionService sessions, AttendanceCaptureService service, CancellationToken ct)
    {
        var employeeId = await sessions.GetEmployeeIdForCurrentUserAsync(context.User);
        if (employeeId is null) return MissingEmployee();
        var result = await service.MarkAsync(employeeId.Value, request.QrToken, request.Action, ct);
        return result.Status == AttendanceCaptureStatus.Success ? TypedResults.Ok(result.Value!) : CaptureProblem(result.Status, result.Message);
    }

    private static async Task<IResult> GetMyMarksTodayAsync(HttpContext context, IdentitySessionService sessions, AttendanceCaptureService service, CancellationToken ct)
    {
        var employeeId = await sessions.GetEmployeeIdForCurrentUserAsync(context.User);
        return employeeId is null ? MissingEmployee() : TypedResults.Ok(await service.GetTodayAsync(employeeId.Value, ct));
    }

    private static IResult MissingEmployee() => TypedResults.Problem("The current User account is not associated with an employee.", statusCode: StatusCodes.Status403Forbidden, title: "Employee association required.");
    private static IResult CaptureProblem(AttendanceCaptureStatus status, string? message) => status switch
    {
        AttendanceCaptureStatus.InactiveCheckpoint => TypedResults.Problem(message ?? "Este checkpoint está desactivado.", statusCode: StatusCodes.Status400BadRequest, title: "Checkpoint unavailable."),
        AttendanceCaptureStatus.ExpiredToken => TypedResults.Problem("El código QR expiró. Escanea el nuevo código.", statusCode: StatusCodes.Status400BadRequest, title: "QR expired."),
        AttendanceCaptureStatus.Replay or AttendanceCaptureStatus.InvalidAction => TypedResults.Problem(message ?? "La marcación ya no es válida.", statusCode: StatusCodes.Status409Conflict, title: "Invalid mark sequence."),
        _ => TypedResults.Problem("El código QR no es válido.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid QR."),
    };

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
