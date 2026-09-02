using Attendance.Api.Modules.Attendance.Domain;
using Attendance.LegacyImporter;
using ClosedXML.Excel;
using Xunit;

namespace Attendance.Api.Tests.LegacyMigration;

public sealed class LegacyExcelReaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"attendance-legacy-{Guid.NewGuid():N}");

    [Fact]
    public void Read_DetectsHeadersByContentAndMapsEverySupportedColumn()
    {
        Directory.CreateDirectory(_directory);
        var file = CreateWorkbook("valid.xlsx", "Ana Torres", new[] { "01/08/2026", "08:00", "12:00", "13:00", "17:00", "14:00", "15:00", "15:30", "16:00" });

        var (marks, validation) = new LegacyExcelReader().Read(file);

        Assert.True(validation.IsValid);
        Assert.Equal(8, marks.Count);
        Assert.Equal(new[] { AttendanceMarkType.Entry, AttendanceMarkType.LunchStart, AttendanceMarkType.LunchEnd, AttendanceMarkType.Exit, AttendanceMarkType.CommissionReturn, AttendanceMarkType.CommissionExit, AttendanceMarkType.OtherReturn, AttendanceMarkType.OtherExit }, marks.Select(x => x.Type));
        Assert.All(marks, mark => Assert.Equal(new DateOnly(2026, 8, 1), mark.LocalDate));
    }

    [Fact]
    public void Read_InvalidHeaderOrTime_BlocksImport()
    {
        Directory.CreateDirectory(_directory);
        var file = CreateWorkbook("invalid.xlsx", "Ana Torres", new[] { "01/08/2026", "08:0x", "", "", "", "", "", "", "" }, replaceHeader: "Ingreso");

        var (_, validation) = new LegacyExcelReader().Read(file);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Blockers, message => message.Contains("no supported attendance header", StringComparison.Ordinal));
    }

    [Fact]
    public void Read_XlsFile_IsExplicitlyRejected()
    {
        Directory.CreateDirectory(_directory);
        var file = Path.Combine(_directory, "legacy.xls");
        File.WriteAllText(file, "not an xls");

        var (_, validation) = new LegacyExcelReader().Read(file);

        Assert.Contains(validation.Blockers, message => message.Contains("Unsupported legacy Excel format", StringComparison.Ordinal));
    }

    [Fact]
    public void ToUtc_UsesLimaOffsetForUnspecifiedLocalTime()
    {
        var value = LegacyTime.ToUtc(new DateOnly(2026, 8, 1), new TimeOnly(8, 0));

        Assert.Equal(new DateTimeOffset(2026, 8, 1, 13, 0, 0, TimeSpan.Zero), value);
    }

    private string CreateWorkbook(string name, string employee, string[] values, string? replaceHeader = null)
    {
        var path = Path.Combine(_directory, name);
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Registro");
        sheet.Cell("B2").Value = employee;
        var headers = new[] { "Fecha", "Entrada", "Inicio Almuerzo", "Fin Almuerzo", "Salida", "Entrada por comisión", "Salida por comisión", "Entrada por otros", "Salida por otros" };
        for (var index = 0; index < headers.Length; index++) sheet.Cell(5, index + 1).Value = index == 0 && replaceHeader is not null ? replaceHeader : headers[index];
        for (var index = 0; index < values.Length; index++) sheet.Cell(6, index + 1).Value = values[index];
        workbook.SaveAs(path);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }
}
