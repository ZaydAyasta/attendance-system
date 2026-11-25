namespace Attendance.Api.Modules.Attendance.Application;

public static class AttendanceRequestValidator
{
    private const int MaxRangeDays = 366;

    public static AttendanceValidationResult<object?> ValidateEmployeeId(Guid employeeId)
    {
        if (employeeId == Guid.Empty)
        {
            return AttendanceValidationResult<object?>.Failure(
                new Dictionary<string, string[]>
                {
                    ["employeeId"] = ["EmployeeId must be a non-empty GUID."]
                });
        }

        return AttendanceValidationResult<object?>.Success(null);
    }

    public static AttendanceValidationResult<DailyAttendanceQuery> ValidateDate(
        DateOnly date)
    {
        if (date == default || date == DateOnly.MaxValue)
        {
            return AttendanceValidationResult<DailyAttendanceQuery>.Failure(
                new Dictionary<string, string[]>
                {
                    ["date"] = ["Date must be after 0001-01-01 and before 9999-12-31."]
                });
        }

        return AttendanceValidationResult<DailyAttendanceQuery>.Success(
            new DailyAttendanceQuery(date));
    }

    public static AttendanceValidationResult<AttendanceRangeQuery> ValidateRange(
        DateOnly? from,
        DateOnly? to)
    {
        var errors = new Dictionary<string, string[]>();

        if (!from.HasValue)
        {
            errors["from"] = ["'from' is required."];
        }
        else if (from.Value == default)
        {
            errors["from"] = ["'from' must be a non-default date."];
        }

        if (!to.HasValue)
        {
            errors["to"] = ["'to' is required."];
        }
        else if (to.Value == default)
        {
            errors["to"] = ["'to' must be a non-default date."];
        }
        else if (to.Value == DateOnly.MaxValue)
        {
            errors["to"] = ["'to' must be before 9999-12-31."];
        }

        if (from.HasValue
            && to.HasValue
            && from.Value != default
            && to.Value != default)
        {
            if (from.Value > to.Value)
            {
                errors["range"] = ["'from' must be less than or equal to 'to'."];
            }
            else if (to.Value.DayNumber - from.Value.DayNumber + 1 > MaxRangeDays)
            {
                errors["range"] =
                [
                    $"'from' and 'to' cannot span more than {MaxRangeDays} days."
                ];
            }
        }

        return errors.Count == 0
            ? AttendanceValidationResult<AttendanceRangeQuery>.Success(
                new AttendanceRangeQuery(from!.Value, to!.Value))
            : AttendanceValidationResult<AttendanceRangeQuery>.Failure(errors);
    }
}
