using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Attendance.LegacyImporter;

public static class LegacyText
{
    public static string NormalizeName(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return string.Join(' ', builder.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
    }

    public static bool TrySplitFullName(string fullName, out string firstName, out string lastName)
    {
        var tokens = fullName.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2)
        {
            firstName = string.Empty;
            lastName = string.Empty;
            return false;
        }

        firstName = tokens[0];
        lastName = string.Join(' ', tokens[1..]);
        return true;
    }

    public static string CreateMarkLegacyId(string employeeCode, DateOnly date, TimeOnly time, string markType)
    {
        var input = $"v1|{employeeCode.Trim()}|{date:yyyy-MM-dd}|{time:HH\\:mm}|{markType}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
