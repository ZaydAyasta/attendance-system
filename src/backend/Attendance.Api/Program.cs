using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.Absences.Application;
using Attendance.Api.Modules.Absences.Endpoints;
using Attendance.Api.Modules.Attendance.Application;
using Attendance.Api.Modules.Attendance.Endpoints;
using Attendance.Api.Modules.WorkCalendar.Application;
using Attendance.Api.Modules.WorkCalendar.Endpoints;
using Attendance.Api.Modules.WorkAssignments.Application;
using Attendance.Api.Modules.WorkAssignments.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<AttendanceDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddAbsencesModule();
builder.Services.AddAttendanceModule(builder.Configuration);
builder.Services.AddWorkCalendarModule();
builder.Services.AddWorkAssignmentsModule();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapAbsenceEndpoints();
app.MapAttendanceEndpoints();
app.MapWorkCalendarEndpoints();
app.MapWorkAssignmentEndpoints();

app.Run();

public partial class Program;
