using System.Data.Common;
using System.Reflection;
using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Attendance.Application;
using Attendance.Api.Modules.Attendance.Domain;
using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.WorkAssignments.Domain;
using Attendance.Api.Modules.WorkCalendar.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Attendance.Api.Tests.Attendance;

public sealed class DailyAttendanceServiceWorkAssignmentTests
{
    private static readonly AttendanceTimeZone AttendanceTimeZone =
        new("America/Lima");

    [Fact]
    public async Task GetByDateAsync_NonWorkingDayWithoutAssignment_ReturnsNonWorkingDay()
    {
        await using var dbContext = await CreateSqliteDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 22);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(WorkCalendarDay.Create(date, DayType.NonWorkingDay, null));
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        Assert.Equal("NonWorkingDay", result.Value!.Status);
    }

    [Fact]
    public async Task GetByDateAsync_NonWorkingDayWithActiveAssignmentAndMarks_ReturnsPresent()
    {
        await using var dbContext = await CreateSqliteDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 22);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(WorkCalendarDay.Create(date, DayType.NonWorkingDay, null));
        dbContext.EmployeeWorkAssignments.Add(
            EmployeeWorkAssignment.Create(employee.Id, date, WorkAssignmentType.WeekendWork, null));
        dbContext.AttendanceMarks.AddRange(
            CreateMark(employee.Id, date, 8, 0, AttendanceMarkType.Entry),
            CreateMark(employee.Id, date, 18, 0, AttendanceMarkType.Exit));
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        Assert.Equal("Present", result.Value!.Status);
    }

    [Fact]
    public async Task GetByDateAsync_NonWorkingDayWithActiveAssignmentWithoutMarks_ReturnsUnexcusedAbsence()
    {
        await using var dbContext = await CreateSqliteDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 22);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(WorkCalendarDay.Create(date, DayType.NonWorkingDay, null));
        dbContext.EmployeeWorkAssignments.Add(
            EmployeeWorkAssignment.Create(employee.Id, date, WorkAssignmentType.WeekendWork, null));
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        Assert.Equal("UnexcusedAbsence", result.Value!.Status);
    }

    [Fact]
    public async Task GetByDateAsync_NonWorkingDayWithCancelledAssignment_ReturnsNonWorkingDay()
    {
        await using var dbContext = await CreateSqliteDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 22);
        var assignment = EmployeeWorkAssignment.Create(
            employee.Id,
            date,
            WorkAssignmentType.WeekendWork,
            null);
        assignment.Cancel();

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(WorkCalendarDay.Create(date, DayType.NonWorkingDay, null));
        dbContext.EmployeeWorkAssignments.Add(assignment);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        Assert.Equal("NonWorkingDay", result.Value!.Status);
    }

    [Fact]
    public async Task GetByDateAsync_WorkingDayWithActiveAssignment_PreservesWorkingDayBehavior()
    {
        await using var dbContext = await CreateSqliteDbContextAsync();
        var employee = CreateEmployee();
        var date = new DateOnly(2026, 8, 24);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.Add(WorkCalendarDay.Create(date, DayType.WorkingDay, null));
        dbContext.EmployeeWorkAssignments.Add(
            EmployeeWorkAssignment.Create(employee.Id, date, WorkAssignmentType.Recovery, null));
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).GetByDateAsync(
            employee.Id,
            new DailyAttendanceQuery(date),
            CancellationToken.None);

        Assert.Equal("UnexcusedAbsence", result.Value!.Status);
    }

    [Fact]
    public async Task GetRangeAsync_MixesDaysWithAndWithoutAssignmentsCorrectly()
    {
        await using var dbContext = await CreateSqliteDbContextAsync();
        var employee = CreateEmployee();
        var from = new DateOnly(2026, 8, 22);
        var to = new DateOnly(2026, 8, 24);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.AddRange(
            WorkCalendarDay.Create(from, DayType.NonWorkingDay, null),
            WorkCalendarDay.Create(from.AddDays(1), DayType.NonWorkingDay, null),
            WorkCalendarDay.Create(to, DayType.WorkingDay, null));
        dbContext.EmployeeWorkAssignments.Add(
            EmployeeWorkAssignment.Create(
                employee.Id,
                from.AddDays(1),
                WorkAssignmentType.WeekendWork,
                null));
        dbContext.AttendanceMarks.AddRange(
            CreateMark(employee.Id, from.AddDays(1), 8, 0, AttendanceMarkType.Entry),
            CreateMark(employee.Id, from.AddDays(1), 18, 0, AttendanceMarkType.Exit));
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).GetRangeAsync(
            employee.Id,
            new AttendanceRangeQuery(from, to),
            CancellationToken.None);

        Assert.Collection(
            result.Value!.Days,
            day => Assert.Equal("NonWorkingDay", day.Status),
            day => Assert.Equal("Present", day.Status),
            day => Assert.Equal("UnexcusedAbsence", day.Status));
    }

    [Fact]
    public async Task GetRangeAsync_UsesFiveQueriesWithoutNPlusOne()
    {
        var interceptor = new QueryCountingInterceptor();
        await using var dbContext = await CreateSqliteDbContextAsync(interceptor);
        var employee = CreateEmployee();
        var from = new DateOnly(2026, 8, 22);
        var to = new DateOnly(2026, 8, 24);

        dbContext.Employees.Add(employee);
        dbContext.WorkCalendarDays.AddRange(
            WorkCalendarDay.Create(from, DayType.NonWorkingDay, null),
            WorkCalendarDay.Create(from.AddDays(1), DayType.NonWorkingDay, null),
            WorkCalendarDay.Create(to, DayType.WorkingDay, null));
        dbContext.EmployeeWorkAssignments.Add(
            EmployeeWorkAssignment.Create(
                employee.Id,
                from.AddDays(1),
                WorkAssignmentType.WeekendWork,
                null));
        await dbContext.SaveChangesAsync();

        interceptor.Reset();

        var result = await CreateService(dbContext).GetRangeAsync(
            employee.Id,
            new AttendanceRangeQuery(from, to),
            CancellationToken.None);

        Assert.Equal(AttendanceQueryStatus.Success, result.Status);
        Assert.Equal(5, interceptor.ExecutedCommands);
    }

    private static async Task<AttendanceDbContext> CreateSqliteDbContextAsync(
        QueryCountingInterceptor? interceptor = null)
    {
        var dbContext = CreateSqliteDbContext(
            new SqliteConnection("Data Source=:memory:"),
            interceptor);
        await dbContext.Database.OpenConnectionAsync();
        await InitializeSchemaAsync(dbContext);

        return dbContext;
    }

    private static AttendanceDbContext CreateSqliteDbContext(
        SqliteConnection connection,
        QueryCountingInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseSqlite(connection);

        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        return new AttendanceDbContext(builder.Options);
    }

    private static async Task InitializeSchemaAsync(AttendanceDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE employees (
                id TEXT NOT NULL CONSTRAINT PK_employees PRIMARY KEY,
                employee_code TEXT NOT NULL,
                first_name TEXT NOT NULL,
                last_name TEXT NOT NULL,
                is_active INTEGER NOT NULL,
                hire_date TEXT NOT NULL,
                termination_date TEXT NULL
            );
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IX_employees_employee_code
            ON employees (employee_code);
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE work_calendar_days (
                id TEXT NOT NULL CONSTRAINT PK_work_calendar_days PRIMARY KEY,
                date TEXT NOT NULL,
                day_type TEXT NOT NULL,
                description TEXT NULL,
                Version INTEGER NOT NULL DEFAULT 1
            );
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IX_work_calendar_days_date
            ON work_calendar_days (date);
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE absences (
                id TEXT NOT NULL CONSTRAINT PK_absences PRIMARY KEY,
                employee_id TEXT NOT NULL,
                absence_type TEXT NOT NULL,
                status TEXT NOT NULL,
                reason TEXT NULL,
                notes TEXT NULL,
                Version INTEGER NOT NULL DEFAULT 1,
                start_date TEXT NOT NULL,
                end_date TEXT NOT NULL,
                CONSTRAINT FK_absences_employees_employee_id
                    FOREIGN KEY (employee_id) REFERENCES employees (id) ON DELETE RESTRICT
            );
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IX_absences_employee_id
            ON absences (employee_id);
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE attendance_marks (
                id TEXT NOT NULL CONSTRAINT PK_attendance_marks PRIMARY KEY,
                employee_id TEXT NOT NULL,
                occurred_at TEXT NOT NULL,
                mark_type TEXT NOT NULL,
                source TEXT NOT NULL,
                checkpoint_id TEXT NULL,
                CONSTRAINT FK_attendance_marks_employees_employee_id
                    FOREIGN KEY (employee_id) REFERENCES employees (id) ON DELETE RESTRICT
            );
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IX_attendance_marks_employee_id_occurred_at
            ON attendance_marks (employee_id, occurred_at);
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE employee_work_assignments (
                id TEXT NOT NULL CONSTRAINT PK_employee_work_assignments PRIMARY KEY,
                employee_id TEXT NOT NULL,
                date TEXT NOT NULL,
                assignment_type TEXT NOT NULL,
                comment TEXT NULL,
                status TEXT NOT NULL,
                Version INTEGER NOT NULL DEFAULT 1,
                CONSTRAINT FK_employee_work_assignments_employees_employee_id
                    FOREIGN KEY (employee_id) REFERENCES employees (id) ON DELETE RESTRICT
            );
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IX_employee_work_assignments_employee_id
            ON employee_work_assignments (employee_id);
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IX_employee_work_assignments_date
            ON employee_work_assignments (date);
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IX_employee_work_assignments_employee_id_date
            ON employee_work_assignments (employee_id, date);
            """);
    }

    private static DailyAttendanceService CreateService(AttendanceDbContext dbContext)
        => new(
            dbContext,
            new AttendanceEvaluator(),
            new AttendanceTimeCalculator(),
            AttendanceTimeZone);

    private static AttendanceMark CreateMark(
        Guid employeeId,
        DateOnly date,
        int hour,
        int minute,
        AttendanceMarkType type)
        => AttendanceMark.Create(
            employeeId,
            new DateTimeOffset(
                date.ToDateTime(new TimeOnly(hour, minute)),
                TimeSpan.Zero),
            type,
            AttendanceSource.Manual,
            null);

    private static Employee CreateEmployee()
    {
        var employee = new Employee();
        var id = Guid.NewGuid();

        SetEmployeeProperty(employee, nameof(Employee.Id), id);
        SetEmployeeProperty(employee, nameof(Employee.EmployeeCode), $"EMP-{id:N}"[..12]);
        SetEmployeeProperty(employee, nameof(Employee.FirstName), "Ana");
        SetEmployeeProperty(employee, nameof(Employee.LastName), "Perez");
        SetEmployeeProperty(employee, nameof(Employee.IsActive), true);
        SetEmployeeProperty(employee, nameof(Employee.HireDate), new DateOnly(2024, 1, 10));
        SetEmployeeProperty(employee, nameof(Employee.TerminationDate), null as DateOnly?);

        return employee;
    }

    private static void SetEmployeeProperty<T>(
        Employee employee,
        string propertyName,
        T value)
        => typeof(Employee)
            .GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(employee, value);

    private sealed class QueryCountingInterceptor : DbCommandInterceptor
    {
        public int ExecutedCommands { get; private set; }

        public void Reset()
            => ExecutedCommands = 0;

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            ExecutedCommands++;
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ExecutedCommands++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
