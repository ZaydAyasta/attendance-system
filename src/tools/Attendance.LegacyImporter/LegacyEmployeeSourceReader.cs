using Npgsql;

namespace Attendance.LegacyImporter;

public sealed class LegacyEmployeeSourceReader
{
    public async Task<IReadOnlyList<LegacyEmployeeSourceRow>> ReadAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction))
        {
            await readOnly.ExecuteNonQueryAsync(cancellationToken);
        }

        const string sql = """
            SELECT p.employee_key, p.employee_name, e.hire_date
            FROM public.employee_profiles p
            LEFT JOIN public.employees e ON e.id = p.employee_id
            ORDER BY p.employee_key
            """;
        var result = new List<LegacyEmployeeSourceRow>();
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new LegacyEmployeeSourceRow(
                    reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    reader.IsDBNull(2) ? null : DateOnly.FromDateTime(reader.GetDateTime(2))));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
