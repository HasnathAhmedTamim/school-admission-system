using System.Net;
using Npgsql;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Api;

[Collection(PostgresCollection.Name)]
public sealed class HoldExpiryTests(PostgresFixture db) : IDisposable
{
    private const int HoldSeconds = 2;
    private readonly ApiFactory _factory = new(db.ConnectionString, holdDurationSeconds: HoldSeconds);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ExpiredHold_ReleasesSeat_AndCannotBeConfirmed()
    {
        var client = _factory.CreateClient();
        var schoolClass = await client.CreateClassAsync(totalSeats: 1);
        var studentId = ApiClient.NewStudentId();
        var hold = await client.ApplySuccessfullyAsync(schoolClass.Id, studentId);
        Assert.Equal(HttpStatusCode.Conflict, (await client.ApplyAsync(schoolClass.Id, ApiClient.NewStudentId())).StatusCode);

        await Task.Delay(TimeSpan.FromSeconds(HoldSeconds + 0.5));

        var status = await (await client.GetHoldAsync(hold.HoldId)).ReadAsync<HoldDto>();
        Assert.Equal("EXPIRED", status.Status);
        Assert.Equal(1, (await client.GetClassAsync(schoolClass.Id)).AvailableSeats);

        var confirm = await client.ConfirmAsync(hold.HoldId, studentId);
        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);
        Assert.Equal("HOLD_EXPIRED", (await confirm.ReadAsync<ErrorDto>()).Error);

        await client.ApplySuccessfullyAsync(schoolClass.Id, studentId);
    }

    [Fact]
    public async Task BackgroundWorker_PersistsExpiredStatus()
    {
        var client = _factory.CreateClient();
        var schoolClass = await client.CreateClassAsync(totalSeats: 3);
        var hold = await client.ApplySuccessfullyAsync(schoolClass.Id, ApiClient.NewStudentId());

        var deadline = DateTime.UtcNow.AddSeconds(HoldSeconds + 15);
        string storedStatus;
        do
        {
            await Task.Delay(500);
            storedStatus = await ReadStoredStatusAsync(hold.HoldId);
        }
        while (storedStatus == "HELD" && DateTime.UtcNow < deadline);

        Assert.Equal("EXPIRED", storedStatus);
    }

    private async Task<string> ReadStoredStatusAsync(string holdId)
    {
        await using var connection = new NpgsqlConnection(db.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT status FROM admission_holds WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", Guid.Parse(holdId));
        return (string)(await command.ExecuteScalarAsync())!;
    }
}
