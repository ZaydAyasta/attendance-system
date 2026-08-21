using Attendance.Api.Modules.WorkAssignments.Domain;
using Attendance.Api.Modules.WorkCalendar.Domain;

namespace Attendance.Api.Modules.Attendance.Application;

public static class AttendanceDayTypeResolver
{
    public static DayType? Resolve(
        WorkCalendarDay? workCalendarDay,
        EmployeeWorkAssignment? activeWorkAssignment)
    {
        if (workCalendarDay is null)
        {
            return null;
        }

        if (activeWorkAssignment is null
            || activeWorkAssignment.Status != WorkAssignmentStatus.Active)
        {
            return workCalendarDay.DayType;
        }

        return workCalendarDay.DayType == DayType.NonWorkingDay
            ? DayType.WorkingDay
            : workCalendarDay.DayType;
    }
}
