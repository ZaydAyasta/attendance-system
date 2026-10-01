using Attendance.Api.BuildingBlocks.Persistence;
using Attendance.Api.Modules.LegacyMigration.Application;
using Attendance.Api.Modules.LegacyMigration.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Attendance.Api.Tests.LegacyMigration;

public sealed class LegacyProductionImportServiceTests
{
    [Fact]
    public async Task ApplyAsync_CreatesRecordsAndMakesAnIdenticalRetrySafe()
    {
        await using var db = CreateContext();
        var service = new LegacyProductionImportService(db);
        var request = Request();

        var first = await service.ApplyAsync(request, CancellationToken.None);
        var retry = await service.ApplyAsync(request, CancellationToken.None);

        Assert.Equal(1, first.CreatedEmployees);
        Assert.Equal(2, first.CreatedMarks);
        Assert.Equal(0, retry.CreatedEmployees);
        Assert.Equal(0, retry.CreatedMarks);
        Assert.Equal(3, retry.SkippedMappings);
        Assert.Equal(1, await db.Employees.CountAsync());
        Assert.Equal(2, await db.AttendanceMarks.CountAsync());
        Assert.Equal(3, await db.LegacyImportMappings.CountAsync());
    }

    [Fact]
    public async Task ApplyAsync_MarkOutsidePayloadRejectsBeforeWritingAnything()
    {
        await using var db = CreateContext();
        var request = Request() with
        {
            Marks = [new LegacyProductionImportMark("UNKNOWN", new DateTimeOffset(2026, 8, 1, 8, 0, 0, TimeSpan.Zero), "Entry", "mark-1")]
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new LegacyProductionImportService(db).ApplyAsync(request, CancellationToken.None));

        Assert.Contains("outside the legacy payload", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, await db.Employees.CountAsync());
        Assert.Equal(0, await db.AttendanceMarks.CountAsync());
    }

    private static AttendanceDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static LegacyProductionImportRequest Request()
        => new(
            [new LegacyProductionImportEmployee("EMP-001", "Ana", "Torres", new DateOnly(2020, 1, 2))],
            [
                new LegacyProductionImportMark("EMP-001", new DateTimeOffset(2026, 8, 1, 8, 0, 0, TimeSpan.Zero), "Entry", "mark-1"),
                new LegacyProductionImportMark("EMP-001", new DateTimeOffset(2026, 8, 1, 17, 0, 0, TimeSpan.Zero), "Exit", "mark-2")
            ]);
}
