using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.WorkCalendar.Application;
using Attendance.Api.Modules.WorkCalendar.Contracts;
using Attendance.Api.Modules.WorkCalendar.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Attendance.Api.Tests.WorkCalendar;

public sealed class WorkCalendarServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesDayCorrectly()
    {
        await using var dbContext = CreateDbContext();
        var service = new WorkCalendarService(dbContext);

        var result = await service.CreateAsync(
            new CreateWorkCalendarDayCommand(
                new DateOnly(2026, 8, 30),
                DayType.Holiday,
                "Feriado nacional"),
            CancellationToken.None);

        Assert.Equal(WorkCalendarWriteStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(new DateOnly(2026, 8, 30), result.Value!.Date);
        Assert.Equal("Holiday", result.Value.DayType);
        Assert.Equal("Feriado nacional", result.Value.Description);
        Assert.Equal(0u, result.Value.Version);

        var entity = await dbContext.WorkCalendarDays
            .SingleAsync(x => x.Date == new DateOnly(2026, 8, 30));

        Assert.Equal(DayType.Holiday, entity.DayType);
        Assert.Equal("Feriado nacional", entity.Description);
    }

    [Fact]
    public async Task CreateAsync_ReturnsDuplicateWhenDateAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        dbContext.WorkCalendarDays.Add(
            WorkCalendarDay.Create(
                new DateOnly(2026, 8, 30),
                DayType.Holiday,
                "Feriado"));
        await dbContext.SaveChangesAsync();

        var service = new WorkCalendarService(dbContext);

        var result = await service.CreateAsync(
            new CreateWorkCalendarDayCommand(
                new DateOnly(2026, 8, 30),
                DayType.WorkingDay,
                "Duplicado"),
            CancellationToken.None);

        Assert.Equal(WorkCalendarWriteStatus.Duplicate, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task ListAsync_FiltersByRangeAndOrdersByDate()
    {
        await using var dbContext = CreateDbContext();
        dbContext.WorkCalendarDays.AddRange(
            WorkCalendarDay.Create(
                new DateOnly(2026, 8, 31),
                DayType.WorkingDay,
                "Fuera de rango"),
            WorkCalendarDay.Create(
                new DateOnly(2026, 8, 30),
                DayType.Holiday,
                "Feriado"),
            WorkCalendarDay.Create(
                new DateOnly(2026, 8, 29),
                DayType.NonWorkingDay,
                "Descanso"));
        await dbContext.SaveChangesAsync();

        var service = new WorkCalendarService(dbContext);

        var result = await service.ListAsync(
            new DateOnly(2026, 8, 29),
            new DateOnly(2026, 8, 30),
            CancellationToken.None);

        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal(new DateOnly(2026, 8, 29), first.Date);
                Assert.Equal("NonWorkingDay", first.DayType);
            },
            second =>
            {
                Assert.Equal(new DateOnly(2026, 8, 30), second.Date);
                Assert.Equal("Holiday", second.DayType);
            });
    }

    [Fact]
    public async Task GetByDateAsync_ReturnsNullWhenDateDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = new WorkCalendarService(dbContext);

        var result = await service.GetByDateAsync(
            new DateOnly(2026, 8, 30),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingDay()
    {
        await using var dbContext = CreateDbContext();
        dbContext.WorkCalendarDays.Add(
            WorkCalendarDay.Create(
                new DateOnly(2026, 8, 30),
                DayType.Holiday,
                "Feriado nacional"));
        await dbContext.SaveChangesAsync();
        SetVersion(
            dbContext,
            await dbContext.WorkCalendarDays.SingleAsync(
                x => x.Date == new DateOnly(2026, 8, 30)),
            10u);
        await dbContext.SaveChangesAsync();

        var service = new WorkCalendarService(dbContext);

        var result = await service.UpdateAsync(
            new DateOnly(2026, 8, 30),
            new UpdateWorkCalendarDayCommand(
                DayType.WorkingDay,
                "Jornada especial",
                10u),
            CancellationToken.None);

        Assert.Equal(WorkCalendarWriteStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal("WorkingDay", result.Value!.DayType);
        Assert.Equal("Jornada especial", result.Value.Description);
        Assert.Equal(10u, result.Value.Version);

        var entity = await dbContext.WorkCalendarDays
            .SingleAsync(x => x.Date == new DateOnly(2026, 8, 30));

        Assert.Equal(DayType.WorkingDay, entity.DayType);
        Assert.Equal("Jornada especial", entity.Description);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConcurrencyConflictWhenVersionIsStale()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var databaseRoot = new InMemoryDatabaseRoot();

        await using var setupContext = CreateDbContext(databaseName, databaseRoot);
        setupContext.WorkCalendarDays.Add(
            WorkCalendarDay.Create(
                new DateOnly(2026, 8, 30),
                DayType.Holiday,
                "Feriado"));
        await setupContext.SaveChangesAsync();

        var existingEntity = await setupContext.WorkCalendarDays
            .SingleAsync(x => x.Date == new DateOnly(2026, 8, 30));
        SetVersion(setupContext, existingEntity, 11u);
        await setupContext.SaveChangesAsync();

        await using var updateContext = CreateDbContext(databaseName, databaseRoot);
        var service = new WorkCalendarService(updateContext);

        var result = await service.UpdateAsync(
            new DateOnly(2026, 8, 30),
            new UpdateWorkCalendarDayCommand(
                DayType.WorkingDay,
                "Jornada especial",
                10u),
            CancellationToken.None);

        Assert.Equal(WorkCalendarWriteStatus.ConcurrencyConflict, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task DeleteAsync_RemovesExistingDay()
    {
        await using var dbContext = CreateDbContext();
        dbContext.WorkCalendarDays.Add(
            WorkCalendarDay.Create(
                new DateOnly(2026, 8, 30),
                DayType.Holiday,
                "Feriado"));
        await dbContext.SaveChangesAsync();

        var service = new WorkCalendarService(dbContext);

        var result = await service.DeleteAsync(
            new DateOnly(2026, 8, 30),
            CancellationToken.None);

        Assert.Equal(WorkCalendarWriteStatus.Success, result.Status);
        Assert.False(await dbContext.WorkCalendarDays
            .AnyAsync(x => x.Date == new DateOnly(2026, 8, 30)));
    }

    [Fact]
    public async Task BulkConfigureAsync_CreatesDaysAndSkipsExistingHolidayWithoutOverwrite()
    {
        await using var dbContext = CreateDbContext();
        dbContext.WorkCalendarDays.Add(WorkCalendarDay.Create(new DateOnly(2026, 8, 20), DayType.Holiday, "Feriado"));
        await dbContext.SaveChangesAsync();
        var service = new WorkCalendarService(dbContext);

        var result = await service.BulkConfigureAsync(new BulkConfigureWorkCalendarCommand(
            [
                new BulkConfigureWorkCalendarDayCommand(new DateOnly(2026, 8, 19), DayType.WorkingDay, null, null),
                new BulkConfigureWorkCalendarDayCommand(new DateOnly(2026, 8, 20), DayType.WorkingDay, null, null),
                new BulkConfigureWorkCalendarDayCommand(new DateOnly(2026, 8, 21), DayType.NonWorkingDay, null, null)
            ], false), CancellationToken.None);

        Assert.Equal(WorkCalendarWriteStatus.Success, result.Status);
        Assert.Equal(new BulkConfigureWorkCalendarResponse(2, 0, 1), result.Value);
        var holiday = await dbContext.WorkCalendarDays.SingleAsync(x => x.Date == new DateOnly(2026, 8, 20));
        Assert.Equal(DayType.Holiday, holiday.DayType);
        Assert.Equal("Feriado", holiday.Description);
    }

    [Fact]
    public async Task BulkConfigureAsync_OverwritesExistingOnlyWithMatchingVersion()
    {
        await using var dbContext = CreateDbContext();
        var day = WorkCalendarDay.Create(new DateOnly(2026, 8, 20), DayType.Holiday, "Feriado");
        dbContext.WorkCalendarDays.Add(day);
        await dbContext.SaveChangesAsync();
        SetVersion(dbContext, day, 7);
        await dbContext.SaveChangesAsync();
        var service = new WorkCalendarService(dbContext);

        var result = await service.BulkConfigureAsync(new BulkConfigureWorkCalendarCommand(
            [new BulkConfigureWorkCalendarDayCommand(day.Date, DayType.WorkingDay, null, 7)], true), CancellationToken.None);

        Assert.Equal(WorkCalendarWriteStatus.Success, result.Status);
        Assert.Equal(new BulkConfigureWorkCalendarResponse(0, 1, 0), result.Value);
        Assert.Equal(DayType.WorkingDay, (await dbContext.WorkCalendarDays.SingleAsync()).DayType);
    }

    private static AttendanceDbContext CreateDbContext()
        => CreateDbContext(Guid.NewGuid().ToString("N"), new InMemoryDatabaseRoot());

    private static AttendanceDbContext CreateDbContext(
        string databaseName,
        InMemoryDatabaseRoot databaseRoot)
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(
                databaseName,
                databaseRoot)
            .Options;

        return new AttendanceDbContext(options);
    }

    private static void SetVersion(
        AttendanceDbContext dbContext,
        WorkCalendarDay entity,
        uint version)
    {
        dbContext.Entry(entity)
            .Property(x => x.Version)
            .CurrentValue = version;
    }
}
