using PdfSharp.Fonts;

namespace Attendance.Api.Modules.Reporting.Application;

internal sealed class AttendanceReportFontResolver : IFontResolver
{
    public const string FamilyName = "Liberation Sans";
    private const string Regular = "LiberationSans-Regular";
    private const string Bold = "LiberationSans-Bold";

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
        => string.Equals(familyName, FamilyName, StringComparison.OrdinalIgnoreCase) && !italic
            ? new FontResolverInfo(bold ? Bold : Regular)
            : null;

    public byte[]? GetFont(string faceName)
    {
        if (faceName is not (Regular or Bold)) return null;

        using var resource = typeof(AttendanceReportFontResolver).Assembly.GetManifestResourceStream(
            $"Attendance.Api.Modules.Reporting.Fonts.{faceName}.ttf")
            ?? throw new InvalidOperationException($"The report font '{faceName}' is missing.");
        using var bytes = new MemoryStream();
        resource.CopyTo(bytes);
        return bytes.ToArray();
    }
}
