using Attendance.Api.Modules.Reporting.Application;
using Attendance.Api.Modules.Reporting.Contracts;
using ClosedXML.Excel;
using Xunit;

namespace Attendance.Api.Tests.Reporting;

public sealed class AttendanceReportExcelTests
{
    [Fact]
    public void Excel_IncludesNakamaLogoAndKeepsTheAttendanceTableBelowTheHeader()
    {
        var report = new AttendanceReportResponse(
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 31),
            null,
            new AttendanceReportSummary(1, 31, 20, 1, 2, 9600),
            [new AttendanceReportRow(Guid.NewGuid(), "Ingenieria - Carlos", "Carlos Abraham Delgado", new DateOnly(2026, 10, 1), "Present", 480, 60, [], null)]);

        var bytes = new AttendanceReportService(null!, null!, null!, null!).Excel(report);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheet("Asistencia");

        var picture = Assert.Single(worksheet.Pictures);
        Assert.Equal("NakamaLogo", picture.Name);
        Assert.Equal("REPORTE DE ASISTENCIA", worksheet.Cell(1, 3).GetString());
        Assert.Equal("Período: 01/10/2026 - 31/10/2026", worksheet.Cell(2, 3).GetString());
        Assert.Equal("Código", worksheet.Cell(5, 1).GetString());
        Assert.Equal("Ingenieria - Carlos", worksheet.Cell(6, 1).GetString());
    }
}
