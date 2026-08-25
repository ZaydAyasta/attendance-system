using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Absences.Application;
using Attendance.Api.Modules.Absences.Endpoints;
using Attendance.Api.Modules.Employees.Application;
using Attendance.Api.Modules.Employees.Endpoints;
using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.Attendance.Application;
using Attendance.Api.Modules.Attendance.Endpoints;
using Attendance.Api.Modules.WorkCalendar.Application;
using Attendance.Api.Modules.WorkCalendar.Endpoints;
using Attendance.Api.Modules.WorkAssignments.Application;
using Attendance.Api.Modules.WorkAssignments.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<AttendanceDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddAbsencesModule();
builder.Services.AddEmployeesModule();
builder.Services.AddAttendanceModule(builder.Configuration);
builder.Services.AddWorkCalendarModule();
builder.Services.AddWorkAssignmentsModule();

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info ??= new OpenApiInfo();
        document.Info.Title = "Sistema de Asistencia API";
        document.Info.Version = "v1";

        return Task.CompletedTask;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
    var seedEmployees = new[]
    {
        (Guid.Parse("b89f8253-79bb-4bb2-98d1-67114482fe01"), "EMP-TEST-001", "María", "Torres"),
        (Guid.Parse("b89f8253-79bb-4bb2-98d1-67114482fe02"), "EMP-TEST-002", "Carlos", "Ramírez"),
        (Guid.Parse("b89f8253-79bb-4bb2-98d1-67114482fe03"), "EMP-TEST-003", "Lucía", "Mendoza"),
        (Guid.Parse("b89f8253-79bb-4bb2-98d1-67114482fe04"), "EMP-TEST-004", "Diego", "Vargas")
    };

    var seedCodes = seedEmployees.Select(seed => seed.Item2).ToArray();
    var existingCodes = await dbContext.Employees
        .Where(x => seedCodes.Contains(x.EmployeeCode))
        .Select(x => x.EmployeeCode)
        .ToListAsync();

    foreach (var (id, code, firstName, lastName) in seedEmployees.Where(seed => !existingCodes.Contains(seed.Item2)))
    {
        var employee = (Employee)Activator.CreateInstance(typeof(Employee), nonPublic: true)!;
        dbContext.Employees.Add(employee);
        dbContext.Entry(employee).Property(x => x.Id).CurrentValue = id;
        dbContext.Entry(employee).Property(x => x.EmployeeCode).CurrentValue = code;
        dbContext.Entry(employee).Property(x => x.FirstName).CurrentValue = firstName;
        dbContext.Entry(employee).Property(x => x.LastName).CurrentValue = lastName;
        dbContext.Entry(employee).Property(x => x.IsActive).CurrentValue = true;
        dbContext.Entry(employee).Property(x => x.HireDate).CurrentValue = new DateOnly(2025, 1, 1);
    }

    await dbContext.SaveChangesAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs", options =>
    {
        options.WithTitle("Sistema de Asistencia API");
        options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapAbsenceEndpoints();
app.MapEmployeeEndpoints();
app.MapAttendanceEndpoints();
app.MapWorkCalendarEndpoints();
app.MapWorkAssignmentEndpoints();

app.Run();

public partial class Program;
