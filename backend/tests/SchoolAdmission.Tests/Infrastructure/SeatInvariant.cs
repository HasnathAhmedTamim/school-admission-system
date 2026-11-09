using Npgsql;

namespace SchoolAdmission.Tests.Infrastructure;

/// <summary>Reads seat usage straight from PostgreSQL, bypassing the API.</summary>
public static class SeatInvariant
{
    public static async Task<(int TotalSeats, int Occupied, int HoldRows)> ReadAsync(string connectionString, string classId)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.total_seats,
                   count(h.id) FILTER (WHERE h.status = 'CONFIRMED' OR (h.status = 'HELD' AND h.expires_at > now())),
                   count(h.id)
            FROM classes c
            LEFT JOIN admission_holds h ON h.class_id = c.id
            WHERE c.id = @id
            GROUP BY c.total_seats
            """;
        command.Parameters.AddWithValue("id", Guid.Parse(classId));

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync(), "Class not found in database");
        return (reader.GetInt32(0), (int)reader.GetInt64(1), (int)reader.GetInt64(2));
    }
}
