using Attendance.Api.Modules.Attendance.Domain;
using ClosedXML.Excel;
using System.Globalization;

namespace Attendance.LegacyImporter;

public sealed class LegacyExcelReader
{
    private static readonly string[] Headers =
    [
        "Fecha", "Entrada", "Inicio Almuerzo", "Fin Almuerzo", "Salida",
        "Entrada por comisión", "Salida por comisión", "Entrada por otros", "Salida por otros"
    ];

    private static readonly IReadOnlyDictionary<int, AttendanceMarkType> MarkTypes = new Dictionary<int, AttendanceMarkType>
    {
        [2] = AttendanceMarkType.Entry,
        [3] = AttendanceMarkType.LunchStart,
        [4] = AttendanceMarkType.LunchEnd,
        [5] = AttendanceMarkType.Exit,
        [6] = AttendanceMarkType.CommissionReturn,
        [7] = AttendanceMarkType.CommissionExit,
        [8] = AttendanceMarkType.OtherReturn,
        [9] = AttendanceMarkType.OtherExit
    };

    public (IReadOnlyList<LegacyExcelMark> Marks, LegacyValidationReport Validation) Read(string path)
    {
        var validation = new LegacyValidationReport();
        var files = FindFiles(path, validation);
        var marks = new List<LegacyExcelMark>();
        foreach (var file in files)
        {
            ReadWorkbook(file, marks, validation);
        }

        return (marks, validation);
    }

    private static IReadOnlyList<string> FindFiles(string path, LegacyValidationReport validation)
    {
        if (File.Exists(path))
        {
            return IsIgnored(path) ? [] : ValidateExtension(path, validation) ? [Path.GetFullPath(path)] : [];
        }

        if (!Directory.Exists(path))
        {
            validation.Blockers.Add("Excel path does not exist.");
            return [];
        }

        var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Where(file => !IsIgnored(file))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return files.Where(file => ValidateExtension(file, validation)).ToArray();
    }

    private static bool IsIgnored(string path) => Path.GetFileName(path).StartsWith("~$", StringComparison.Ordinal);

    private static bool ValidateExtension(string file, LegacyValidationReport validation)
    {
        var extension = Path.GetExtension(file);
        if (string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase))
        {
            validation.Blockers.Add($"Unsupported legacy Excel format '.xls': {Path.GetFileName(file)}.");
        }
        else
        {
            validation.Warnings.Add($"Ignored non-Excel file: {Path.GetFileName(file)}.");
        }
        return false;
    }

    private static void ReadWorkbook(string file, List<LegacyExcelMark> marks, LegacyValidationReport validation)
    {
        using var workbook = new XLWorkbook(file);
        foreach (var worksheet in workbook.Worksheets)
        {
            var employeeName = worksheet.Cell("B2").GetString().Trim();
            if (string.IsNullOrWhiteSpace(employeeName))
            {
                validation.Blockers.Add($"Worksheet '{worksheet.Name}' has no employee name in B2.");
                continue;
            }

            var headerRows = worksheet.RowsUsed().Where(IsHeaderRow).Select(row => row.RowNumber()).ToArray();
            if (headerRows.Length == 0)
            {
                validation.Blockers.Add($"Worksheet '{worksheet.Name}' has no supported attendance header.");
                continue;
            }

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            foreach (var headerRow in headerRows)
            {
                var end = headerRows.FirstOrDefault(row => row > headerRow) - 1;
                if (end < headerRow) end = lastRow;
                for (var rowNumber = headerRow + 1; rowNumber <= end; rowNumber++)
                {
                    var dateText = worksheet.Cell(rowNumber, 1).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(dateText)) continue;
                    if (!DateOnly.TryParseExact(dateText, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    {
                        if (MarkTypes.Keys.Any(column => !string.IsNullOrWhiteSpace(worksheet.Cell(rowNumber, column).GetString())))
                        {
                            validation.Blockers.Add($"Invalid date in {Path.GetFileName(file)} row {rowNumber}.");
                        }
                        continue;
                    }

                    foreach (var (column, type) in MarkTypes)
                    {
                        var timeText = worksheet.Cell(rowNumber, column).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(timeText)) continue;
                        if (!TimeOnly.TryParseExact(timeText, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                        {
                            validation.Blockers.Add($"Invalid time in {Path.GetFileName(file)} row {rowNumber}, column {column}.");
                            continue;
                        }

                        marks.Add(new LegacyExcelMark(employeeName, LegacyText.NormalizeName(employeeName), date, time, type, file, worksheet.Name, rowNumber));
                    }
                }
            }
        }
    }

    private static bool IsHeaderRow(IXLRow row)
        => Headers.Select((header, index) => row.Cell(index + 1).GetString().Trim() == header).All(match => match);
}
