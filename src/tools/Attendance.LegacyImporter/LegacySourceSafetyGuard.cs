using Npgsql;

namespace Attendance.LegacyImporter;

public static class LegacySourceSafetyGuard
{
    public const string RequiredSourceUser = "legacy_migration_reader";

    public sealed record SourcePermissions(
        string CurrentUser,
        bool CanSelectEmployeeProfiles,
        bool CanInsertEmployeeProfiles,
        bool CanUpdateEmployeeProfiles,
        bool CanDeleteEmployeeProfiles);

    public static async Task<SourcePermissions> EnsureSafeAsync(
        NpgsqlConnection source,
        DatabaseIdentity destination,
        CancellationToken cancellationToken)
    {
        var sourceIdentity = LegacyConnectionSettings.ToIdentity(new NpgsqlConnectionStringBuilder(source.ConnectionString));
        ValidateEndpoints(sourceIdentity, destination);

        await using var transaction = await source.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        await using (var readOnly = source.CreateCommand())
        {
            readOnly.Transaction = transaction;
            readOnly.CommandText = "SET TRANSACTION READ ONLY";
            await readOnly.ExecuteNonQueryAsync(cancellationToken);
        }

        SourcePermissions permissions;
        await using (var command = source.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                SELECT current_user,
                       has_table_privilege(current_user, 'public.employee_profiles', 'SELECT'),
                       has_table_privilege(current_user, 'public.employee_profiles', 'INSERT'),
                       has_table_privilege(current_user, 'public.employee_profiles', 'UPDATE'),
                       has_table_privilege(current_user, 'public.employee_profiles', 'DELETE');
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("The legacy source did not return its effective permissions.");
            }

            permissions = new SourcePermissions(
                reader.GetString(0),
                reader.GetBoolean(1),
                reader.GetBoolean(2),
                reader.GetBoolean(3),
                reader.GetBoolean(4));
        }

        await transaction.RollbackAsync(cancellationToken);
        if (!string.Equals(permissions.CurrentUser, RequiredSourceUser, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The source must authenticate as {RequiredSourceUser}.");
        }

        if (!permissions.CanSelectEmployeeProfiles
            || permissions.CanInsertEmployeeProfiles
            || permissions.CanUpdateEmployeeProfiles
            || permissions.CanDeleteEmployeeProfiles)
        {
            throw new InvalidOperationException("The legacy source must be SELECT-only for public.employee_profiles.");
        }

        return permissions;
    }

    public static void ValidateEndpoints(DatabaseIdentity source, DatabaseIdentity destination)
    {
        if (SameDatabase(source, destination))
        {
            throw new InvalidOperationException("The legacy source and attendance_dev destination identify the same database.");
        }

        if (!IsLocalHost(destination.Host) || !string.Equals(destination.Database, "attendance_dev", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The destination must be local attendance_dev.");
        }
    }

    public static bool SameDatabase(DatabaseIdentity source, DatabaseIdentity destination)
        => string.Equals(source.Host, destination.Host, StringComparison.OrdinalIgnoreCase)
           && source.Port == destination.Port
           && string.Equals(source.Database, destination.Database, StringComparison.OrdinalIgnoreCase);

    public static bool IsLocalHost(string host)
        => string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
           || string.Equals(host, "127.0.0.1", StringComparison.Ordinal)
           || string.Equals(host, "::1", StringComparison.Ordinal);
}
