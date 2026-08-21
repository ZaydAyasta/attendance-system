using Attendance.Api.Modules.WorkAssignments.Contracts;
using Attendance.Api.Modules.WorkAssignments.Domain;

namespace Attendance.Api.Modules.WorkAssignments.Application;

public static class WorkAssignmentRequestValidator
{
    private static readonly string[] AllowedStatuses =
    [
        nameof(WorkAssignmentStatus.Active),
        nameof(WorkAssignmentStatus.Cancelled)
    ];

    private static readonly string[] AllowedTypes =
    [
        nameof(WorkAssignmentType.WeekendWork),
        nameof(WorkAssignmentType.Recovery),
        nameof(WorkAssignmentType.TemporaryWork)
    ];

    public static WorkAssignmentValidationResult<object?> ValidateId(Guid id)
    {
        if (id == Guid.Empty)
        {
            return WorkAssignmentValidationResult<object?>.Failure(
                new Dictionary<string, string[]>
                {
                    ["id"] = ["Id must be a non-empty GUID."]
                });
        }

        return WorkAssignmentValidationResult<object?>.Success(null);
    }

    public static WorkAssignmentValidationResult<object?> ValidateEmployeeId(Guid employeeId)
    {
        if (employeeId == Guid.Empty)
        {
            return WorkAssignmentValidationResult<object?>.Failure(
                new Dictionary<string, string[]>
                {
                    ["employeeId"] = ["EmployeeId must be a non-empty GUID."]
                });
        }

        return WorkAssignmentValidationResult<object?>.Success(null);
    }

    public static WorkAssignmentValidationResult<WorkAssignmentQueryFilters> ValidateList(
        Guid? employeeId,
        DateOnly? from,
        DateOnly? to,
        string? status,
        string? type)
    {
        var errors = new Dictionary<string, string[]>();

        if (employeeId.HasValue && employeeId.Value == Guid.Empty)
        {
            errors["employeeId"] = ["EmployeeId must be a non-empty GUID."];
        }

        ValidateDateRange(from, to, errors);

        var parsedStatus = ParseOptionalStatus(status, errors);
        var parsedType = ParseOptionalType(type, errors);

        return errors.Count == 0
            ? WorkAssignmentValidationResult<WorkAssignmentQueryFilters>.Success(
                new WorkAssignmentQueryFilters(
                    employeeId,
                    from,
                    to,
                    parsedStatus,
                    parsedType))
            : WorkAssignmentValidationResult<WorkAssignmentQueryFilters>.Failure(errors);
    }

    public static WorkAssignmentValidationResult<CreateWorkAssignmentCommand> ValidateCreate(
        CreateWorkAssignmentRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        ValidateEmployeeIdValue(request.EmployeeId, errors);
        ValidateDate(request.Date, "date", errors);
        ValidateComment(request.Comment, errors);

        var type = ParseRequiredType(request.Type, errors);

        return errors.Count == 0
            ? WorkAssignmentValidationResult<CreateWorkAssignmentCommand>.Success(
                new CreateWorkAssignmentCommand(
                    request.EmployeeId,
                    request.Date,
                    type,
                    NormalizeText(request.Comment)))
            : WorkAssignmentValidationResult<CreateWorkAssignmentCommand>.Failure(errors);
    }

    public static WorkAssignmentValidationResult<UpdateWorkAssignmentCommand> ValidateUpdate(
        UpdateWorkAssignmentRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        ValidateDate(request.Date, "date", errors);
        ValidateComment(request.Comment, errors);

        if (request.Version == default)
        {
            errors["version"] = ["Version must be provided and greater than zero."];
        }

        var type = ParseRequiredType(request.Type, errors);

        return errors.Count == 0
            ? WorkAssignmentValidationResult<UpdateWorkAssignmentCommand>.Success(
                new UpdateWorkAssignmentCommand(
                    request.Date,
                    type,
                    NormalizeText(request.Comment),
                    request.Version))
            : WorkAssignmentValidationResult<UpdateWorkAssignmentCommand>.Failure(errors);
    }

    public static WorkAssignmentValidationResult<CancelWorkAssignmentCommand> ValidateCancel(
        CancelWorkAssignmentRequest request)
    {
        if (request.Version == default)
        {
            return WorkAssignmentValidationResult<CancelWorkAssignmentCommand>.Failure(
                new Dictionary<string, string[]>
                {
                    ["version"] = ["Version must be provided and greater than zero."]
                });
        }

        return WorkAssignmentValidationResult<CancelWorkAssignmentCommand>.Success(
            new CancelWorkAssignmentCommand(request.Version));
    }

    private static void ValidateEmployeeIdValue(
        Guid employeeId,
        Dictionary<string, string[]> errors)
    {
        if (employeeId == Guid.Empty)
        {
            errors["employeeId"] = ["EmployeeId must be a non-empty GUID."];
        }
    }

    private static void ValidateDate(
        DateOnly? date,
        string fieldName,
        Dictionary<string, string[]> errors)
    {
        if (date.HasValue && date.Value == default)
        {
            errors[fieldName] = [$"{fieldName[..1].ToUpperInvariant()}{fieldName[1..]} must be a non-default date."];
        }
    }

    private static void ValidateDateRange(
        DateOnly? from,
        DateOnly? to,
        Dictionary<string, string[]> errors)
    {
        ValidateDate(from, "from", errors);
        ValidateDate(to, "to", errors);

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            errors["range"] = ["From must be less than or equal to To."];
        }
    }

    private static void ValidateComment(
        string? comment,
        Dictionary<string, string[]> errors)
    {
        var normalizedComment = NormalizeText(comment);

        if (normalizedComment is not null
            && normalizedComment.Length > EmployeeWorkAssignment.CommentMaxLength)
        {
            errors["comment"] =
            [
                $"Comment cannot exceed {EmployeeWorkAssignment.CommentMaxLength} characters."
            ];
        }
    }

    private static WorkAssignmentType ParseRequiredType(
        string? rawValue,
        Dictionary<string, string[]> errors)
    {
        if (!TryParseType(rawValue, out var type))
        {
            errors["type"] =
            [
                $"Type must be one of: {string.Join(", ", AllowedTypes)}."
            ];
        }

        return type;
    }

    private static WorkAssignmentType? ParseOptionalType(
        string? rawValue,
        Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (!TryParseType(rawValue, out var type))
        {
            errors["type"] =
            [
                $"Type must be one of: {string.Join(", ", AllowedTypes)}."
            ];
        }

        return type;
    }

    private static WorkAssignmentStatus? ParseOptionalStatus(
        string? rawValue,
        Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (!Enum.TryParse<WorkAssignmentStatus>(rawValue, true, out var parsedStatus)
            || !Enum.IsDefined(parsedStatus))
        {
            errors["status"] =
            [
                $"Status must be one of: {string.Join(", ", AllowedStatuses)}."
            ];

            return null;
        }

        return parsedStatus;
    }

    private static bool TryParseType(
        string? rawValue,
        out WorkAssignmentType type)
        => Enum.TryParse(rawValue, true, out type)
           && Enum.IsDefined(type);

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
