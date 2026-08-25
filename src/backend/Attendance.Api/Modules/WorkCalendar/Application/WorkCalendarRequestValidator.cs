using Attendance.Api.Modules.WorkCalendar.Contracts;
using Attendance.Api.Modules.WorkCalendar.Domain;

namespace Attendance.Api.Modules.WorkCalendar.Application;

public static class WorkCalendarRequestValidator
{
    public const int BulkMaxDays = 366;
    private static readonly string[] AllowedDayTypes =
    [
        nameof(DayType.WorkingDay),
        nameof(DayType.NonWorkingDay),
        nameof(DayType.Holiday)
    ];

    public static WorkCalendarValidationResult<object?> ValidateDate(DateOnly date)
    {
        if (date == default)
        {
            return WorkCalendarValidationResult<object?>.Failure(
                new Dictionary<string, string[]>
                {
                    ["date"] = ["Date must be a non-default value."]
                });
        }

        return WorkCalendarValidationResult<object?>.Success(null);
    }

    public static WorkCalendarValidationResult<object?> ValidateRange(
        DateOnly? from,
        DateOnly? to)
    {
        var errors = new Dictionary<string, string[]>();

        if (from.HasValue && from.Value == default)
        {
            errors["from"] = ["'from' must be a non-default date."];
        }

        if (to.HasValue && to.Value == default)
        {
            errors["to"] = ["'to' must be a non-default date."];
        }

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            errors["range"] = ["'from' must be less than or equal to 'to'."];
        }

        return errors.Count == 0
            ? WorkCalendarValidationResult<object?>.Success(null)
            : WorkCalendarValidationResult<object?>.Failure(errors);
    }

    public static WorkCalendarValidationResult<CreateWorkCalendarDayCommand>
        ValidateCreate(CreateWorkCalendarDayRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Date == default)
        {
            errors["date"] = ["Date must be a non-default value."];
        }

        ValidateDescription(request.Description, errors);

        if (!TryParseDayType(request.DayType, out var dayType))
        {
            errors["dayType"] =
            [
                $"DayType must be one of: {string.Join(", ", AllowedDayTypes)}."
            ];
        }

        return errors.Count == 0
            ? WorkCalendarValidationResult<CreateWorkCalendarDayCommand>.Success(
                new CreateWorkCalendarDayCommand(
                    request.Date,
                    dayType,
                    NormalizeDescription(request.Description)))
            : WorkCalendarValidationResult<CreateWorkCalendarDayCommand>.Failure(
                errors);
    }

    public static WorkCalendarValidationResult<UpdateWorkCalendarDayCommand>
        ValidateUpdate(UpdateWorkCalendarDayRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        ValidateDescription(request.Description, errors);

        if (request.Version == default)
        {
            errors["version"] =
            [
                "Version must be provided and greater than zero."
            ];
        }

        if (!TryParseDayType(request.DayType, out var dayType))
        {
            errors["dayType"] =
            [
                $"DayType must be one of: {string.Join(", ", AllowedDayTypes)}."
            ];
        }

        return errors.Count == 0
            ? WorkCalendarValidationResult<UpdateWorkCalendarDayCommand>.Success(
                new UpdateWorkCalendarDayCommand(
                    dayType,
                    NormalizeDescription(request.Description),
                    request.Version))
            : WorkCalendarValidationResult<UpdateWorkCalendarDayCommand>.Failure(
                errors);
    }

    public static WorkCalendarValidationResult<BulkConfigureWorkCalendarCommand>
        ValidateBulkConfigure(BulkConfigureWorkCalendarRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var days = request.Days;
        if (days is null || days.Count == 0)
            errors["days"] = ["At least one day must be provided."];
        else if (days.Count > BulkMaxDays)
            errors["days"] = [$"A bulk request cannot contain more than {BulkMaxDays} days."];

        if (days is not null && days.Count > 0)
        {
            if (days.GroupBy(x => x.Date).Any(x => x.Count() > 1))
                errors["days"] = ["Dates must not be duplicated in a bulk request."];

            for (var index = 0; index < days.Count; index++)
            {
                var day = days[index];
                if (day.Date == default)
                    errors[$"days[{index}].date"] = ["Date must be a non-default value."];
                ValidateDescription(day.Description, errors, $"days[{index}].description");
                if (!TryParseDayType(day.DayType, out _))
                    errors[$"days[{index}].dayType"] =
                        [$"DayType must be one of: {string.Join(", ", AllowedDayTypes)}."];
            }
        }

        if (errors.Count > 0)
            return WorkCalendarValidationResult<BulkConfigureWorkCalendarCommand>.Failure(errors);

        return WorkCalendarValidationResult<BulkConfigureWorkCalendarCommand>.Success(
            new BulkConfigureWorkCalendarCommand(days!.Select(day =>
            {
                TryParseDayType(day.DayType, out var dayType);
                return new BulkConfigureWorkCalendarDayCommand(
                    day.Date, dayType, NormalizeDescription(day.Description), day.Version);
            }).ToList(), request.OverwriteExisting));
    }

    private static void ValidateDescription(
        string? description,
        Dictionary<string, string[]> errors,
        string key = "description")
    {
        var normalizedDescription = NormalizeDescription(description);

        if (normalizedDescription is not null
            && normalizedDescription.Length > WorkCalendarDay.DescriptionMaxLength)
        {
            errors[key] =
            [
                $"Description cannot exceed {WorkCalendarDay.DescriptionMaxLength} characters."
            ];
        }
    }

    private static bool TryParseDayType(string? rawValue, out DayType dayType)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            dayType = default;
            return false;
        }

        return Enum.TryParse(rawValue.Trim(), true, out dayType)
            && Enum.IsDefined(dayType);
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }
}
