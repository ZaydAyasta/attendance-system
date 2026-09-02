using Npgsql;

namespace Attendance.LegacyImporter;

public sealed record DatabaseIdentity(string Host, int Port, string Database, string Username)
{
    public string DisplayName => $"host={Redact(Host)}, database={Redact(Database)}, user={Redact(Username)}";

    private static string Redact(string value)
        => string.IsNullOrWhiteSpace(value) ? "<unset>" : $"{value[..1]}***";
}

public sealed record LegacyConnectionSettings(
    string SourceConnectionString,
    string DestinationConnectionString,
    DatabaseIdentity Source,
    DatabaseIdentity Destination)
{
    public static LegacyConnectionSettings FromEnvironment()
    {
        var source = Environment.GetEnvironmentVariable("LEGACY_DATABASE_URL")
            ?? ReadLocalEnvironmentValue("LEGACY_DATABASE_URL");
        var destination = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new InvalidOperationException("LEGACY_DATABASE_URL must be defined.");
        }

        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new InvalidOperationException("ConnectionStrings__DefaultConnection must be defined for attendance_dev.");
        }

        var sourceBuilder = ParsePostgreSqlConnection(source);
        var destinationBuilder = new NpgsqlConnectionStringBuilder(destination);

        return new LegacyConnectionSettings(
            sourceBuilder.ConnectionString,
            destination,
            ToIdentity(sourceBuilder),
            ToIdentity(destinationBuilder));
    }

    internal static DatabaseIdentity ToIdentity(NpgsqlConnectionStringBuilder builder)
        => new(
            builder.Host ?? string.Empty,
            builder.Port,
            builder.Database ?? string.Empty,
            builder.Username ?? string.Empty);

    private static string? ReadLocalEnvironmentValue(string key)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "local-data", ".env");
        if (!File.Exists(path))
        {
            return null;
        }

        var prefix = $"{key}=";
        var line = File.ReadLines(path)
            .FirstOrDefault(value => value.StartsWith(prefix, StringComparison.Ordinal));

        return line is null ? null : line[prefix.Length..].Trim().Trim('"', '\'');
    }

    private static NpgsqlConnectionStringBuilder ParsePostgreSqlConnection(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return new NpgsqlConnectionStringBuilder(value);
        }

        var credentials = uri.UserInfo.Split(':', 2);
        if (credentials.Length != 2 || string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/')))
        {
            throw new InvalidOperationException("LEGACY_DATABASE_URL is not a complete PostgreSQL URL.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
            SslMode = SslMode.Require,
            GssEncryptionMode = GssEncryptionMode.Disable
        };

        foreach (var item in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = item.Split('=', 2);
            if (pair.Length == 2 && string.Equals(pair[0], "sslmode", StringComparison.OrdinalIgnoreCase))
            {
                builder.SslMode = Enum.Parse<SslMode>(Uri.UnescapeDataString(pair[1]), ignoreCase: true);
            }
        }

        return builder;
    }
}
