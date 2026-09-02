using Npgsql;

namespace Attendance.LegacyImporter;

public sealed record LegacyTableSummary(string Schema, string Name, int ColumnCount);

public sealed record LegacySchemaReport(string CurrentUser, IReadOnlyList<LegacyTableSummary> Tables);

public sealed class LegacySchemaDiscoveryService
{
    public async Task<LegacySchemaReport> InspectAsync(NpgsqlConnection source, CancellationToken cancellationToken)
    {
        await using var transaction = await source.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        await using (var readOnly = source.CreateCommand())
        {
            readOnly.Transaction = transaction;
            readOnly.CommandText = "SET TRANSACTION READ ONLY";
            await readOnly.ExecuteNonQueryAsync(cancellationToken);
        }

        string currentUser;
        await using (var userCommand = source.CreateCommand())
        {
            userCommand.Transaction = transaction;
            userCommand.CommandText = "SELECT current_user";
            currentUser = (string)(await userCommand.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException("The legacy source did not return current_user."));
        }

        var tables = new List<LegacyTableSummary>();
        await using (var tablesCommand = source.CreateCommand())
        {
            tablesCommand.Transaction = transaction;
            tablesCommand.CommandText = """
                SELECT table_schema, table_name, count(*)::int AS column_count
                FROM information_schema.columns
                WHERE table_schema = 'public'
                GROUP BY table_schema, table_name
                ORDER BY table_name;
                """;

            await using var reader = await tablesCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                tables.Add(new LegacyTableSummary(reader.GetString(0), reader.GetString(1), reader.GetInt32(2)));
            }
        }

        await transaction.RollbackAsync(cancellationToken);
        return new LegacySchemaReport(currentUser, tables);
    }
}
