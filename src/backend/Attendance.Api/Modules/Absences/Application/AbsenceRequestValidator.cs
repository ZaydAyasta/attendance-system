using Attendance.Api.Modules.Absences.Contracts;
using Attendance.Api.Modules.Absences.Domain;

namespace Attendance.Api.Modules.Absences.Application;

public static class AbsenceRequestValidator
{
    private static readonly string[] AllowedStatuses =
    [
        nameof(AbsenceStatus.Pending),
        nameof(AbsenceStatus.Approved),
        nameof(AbsenceStatus.Rejected),
        nameof(AbsenceStatus.Cancelled)
    ];

    private static readonly string[] AllowedTypes =
    [
        nameof(AbsenceType.Vacation),
        nameof(AbsenceType.MedicalLeave),
        nameof(AbsenceType.Commission),
        nameof(AbsenceType.JustifiedAbsence),
        nameof(AbsenceType.Permission)
    ];

    public static AbsenceValidationResult<object?> ValidateId(Guid id)
    {
        if (id == Guid.Empty)
        {
            return AbsenceValidationResult<object?>.Failure(
                new Dictionary<string, string[]>
                {
                    ["id"] = ["Id must be a non-empty GUID."]
                });
        }

        return AbsenceValidationResult<object?>.Success(null);
    }

    public static AbsenceValidationResult<object?> ValidateEmployeeId(Guid employeeId)
    {
        if (employeeId == Guid.Empty)
        {
            return AbsenceValidationResult<object?>.Failure(
                new Dictionary<string, string[]>
                {
                    ["employeeId"] = ["EmployeeId must be a non-empty GUID."]
                });
        }

        return AbsenceValidationResult<object?>.Success(null);
    }

    public static AbsenceValidationResult<AbsenceQueryFilters> ValidateList(
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
            ? AbsenceValidationResult<AbsenceQueryFilters>.Success(
                new AbsenceQueryFilters(
                    employeeId,
                    from,
                    to,
                    parsedStatus,
                    parsedType))
            : AbsenceValidationResult<AbsenceQueryFilters>.Failure(errors);
    }

    public static AbsenceValidationResult<CreateAbsenceCommand> ValidateCreate(
        CreateAbsenceRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        ValidateEmployeeIdValue(request.EmployeeId, errors);
        ValidateDateRange(request.StartDate, request.EndDate, errors);
        ValidateReason(request.Reason, errors);
        ValidateNotes(request.Notes, errors);

        var type = ParseRequiredType(request.Type, errors);
        var status = ParseRequiredStatus(request.Status, errors);

        return errors.Count == 0
            ? AbsenceValidationResult<CreateAbsenceCommand>.Success(
                new CreateAbsenceCommand(
                    request.EmployeeId,
                    new DateRange(request.StartDate, request.EndDate),
                    type,
                    status,
                    NormalizeText(request.Reason),
                    NormalizeText(request.Notes)))
            : AbsenceValidationResult<CreateAbsenceCommand>.Failure(errors);
    }

    public static AbsenceValidationResult<UpdateAbsenceCommand> ValidateUpdate(
        UpdateAbsenceRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        ValidateDateRange(request.StartDate, request.EndDate, errors);
        ValidateReason(request.Reason, errors);
        ValidateNotes(request.Notes, errors);

        if (request.Version == default)
        {
            errors["version"] = ["Version must be provided and greater than zero."];
        }

        var type = ParseRequiredType(request.Type, errors);
        var status = ParseRequiredStatus(request.Status, errors);

        return errors.Count == 0
            ? AbsenceValidationResult<UpdateAbsenceCommand>.Success(
                new UpdateAbsenceCommand(
                    new DateRange(request.StartDate, request.EndDate),
                    type,
                    status,
                    NormalizeText(request.Reason),
                    NormalizeText(request.Notes),
                    request.Version))
            : AbsenceValidationResult<UpdateAbsenceCommand>.Failure(errors);
    }

    public static AbsenceValidationResult<CancelAbsenceCommand> ValidateCancel(
        CancelAbsenceRequest request)
    {
        if (request.Version == default)
        {
            return AbsenceValidationResult<CancelAbsenceCommand>.Failure(
                new Dictionary<string, string[]>
                {
                    ["version"] = ["Version must be provided and greater than zero."]
                });
        }

        return AbsenceValidationResult<CancelAbsenceCommand>.Success(
            new CancelAbsenceCommand(request.Version));
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

    private static void ValidateDateRange(
        DateOnly? startDate,
        DateOnly? endDate,
        Dictionary<string, string[]> errors)
    {
        if (startDate.HasValue && startDate.Value == default)
        {
            errors["startDate"] = ["StartDate must be a non-default date."];
        }

        if (endDate.HasValue && endDate.Value == default)
        {
            errors["endDate"] = ["EndDate must be a non-default date."];
        }

        if (!startDate.HasValue)
        {
            return;
        }

        if (!endDate.HasValue)
        {
            return;
        }

        if (startDate.Value > endDate.Value)
        {
            errors["range"] = ["StartDate must be less than or equal to EndDate."];
        }
    }

    private static void ValidateReason(
        string? reason,
        Dictionary<string, string[]> errors)
    {
        var normalizedReason = NormalizeText(reason);

        if (normalizedReason is not null
            && normalizedReason.Length > Absence.ReasonMaxLength)
        {
            errors["reason"] =
            [
                $"Reason cannot exceed {Absence.ReasonMaxLength} characters."
            ];
        }
    }

    private static void ValidateNotes(
        string? notes,
        Dictionary<string, string[]> errors)
    {
        var normalizedNotes = NormalizeText(notes);

        if (normalizedNotes is not null
            && normalizedNotes.Length > Absence.NotesMaxLength)
        {
            errors["notes"] =
            [
                $"Notes cannot exceed {Absence.NotesMaxLength} characters."
            ];
        }
    }

    private static AbsenceType ParseRequiredType(
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

    private static AbsenceStatus ParseRequiredStatus(
        string? rawValue,
        Dictionary<string, string[]> errors)
    {
        if (!TryParseStatus(rawValue, out var status))
        {
            errors["status"] =
            [
                $"Status must be one of: {string.Join(", ", AllowedStatuses)}."
            ];
        }

        return status;
    }

    private static AbsenceType? ParseOptionalType(
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

    private static AbsenceStatus? ParseOptionalStatus(
        string? rawValue,
        Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (!TryParseStatus(rawValue, out var status))
        {
            errors["status"] =
            [
                $"Status must be one of: {string.Join(", ", AllowedStatuses)}."
            ];
        }

        return status;
    }

    private static bool TryParseType(string? rawValue, out AbsenceType type)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            type = default;
            return false;
        }

        return Enum.TryParse(rawValue.Trim(), true, out type)
            && Enum.IsDefined(type);
    }

    private static bool TryParseStatus(string? rawValue, out AbsenceStatus status)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            status = default;
            return false;
        }

        return Enum.TryParse(rawValue.Trim(), true, out status)
            && Enum.IsDefined(status);
    }

    private static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
