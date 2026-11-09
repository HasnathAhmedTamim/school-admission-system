using System.Net;
using Npgsql;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Concurrency;

[Collection(PostgresCollection.Name)]
public sealed class ConcurrencyTests(PostgresFixture db) : IDisposable
{
    private readonly ApiFactory _factory = new(db.ConnectionString);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task SimultaneousApplications_NeverExceedTotalSeats()
    {
        const int seats = 10;
        const int requests = 100;
        var client = _factory.CreateClient();
        var schoolClass = await client.CreateClassAsync(seats, "Concurrency");

        var statuses = await FireSimultaneouslyAsync(requests, _ =>
            client.ApplyAsync(schoolClass.Id, ApiClient.NewStudentId()));

        Assert.Equal(seats, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(requests - seats, statuses.Count(s => s == HttpStatusCode.Conflict));
        await AssertInvariantAsync(schoolClass.Id, expectedOccupied: seats);

        var stats = await client.GetClassAsync(schoolClass.Id);
        Assert.Equal((seats, 0, 0), (stats.HeldSeats, stats.ConfirmedSeats, stats.AvailableSeats));
    }

    [Fact]
    public async Task SimultaneousApplicationsFromSameStudent_CreateExactlyOneHold()
    {
        var client = _factory.CreateClient();
        var schoolClass = await client.CreateClassAsync(totalSeats: 5, "Same student");
        var studentId = ApiClient.NewStudentId();

        var statuses = await FireSimultaneouslyAsync(20, _ => client.ApplyAsync(schoolClass.Id, studentId));

        Assert.Equal(1, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.All(statuses.Where(s => s != HttpStatusCode.Created), s => Assert.Equal(HttpStatusCode.Conflict, s));
        await AssertInvariantAsync(schoolClass.Id, expectedOccupied: 1);
    }

    [Fact]
    public async Task SimultaneousConfirmations_CountTheSeatOnce()
    {
        var client = _factory.CreateClient();
        var schoolClass = await client.CreateClassAsync(totalSeats: 3, "Confirm race");
        var studentId = ApiClient.NewStudentId();
        var hold = await client.ApplySuccessfullyAsync(schoolClass.Id, studentId);

        var statuses = await FireSimultaneouslyAsync(30, _ => client.ConfirmAsync(hold.HoldId, studentId));

        Assert.All(statuses, s => Assert.Equal(HttpStatusCode.OK, s));
        var stats = await client.GetClassAsync(schoolClass.Id);
        Assert.Equal((0, 1, 2), (stats.HeldSeats, stats.ConfirmedSeats, stats.AvailableSeats));
    }

    [Fact]
    public async Task TwoApiInstancesSharingTheDatabase_NeverExceedTotalSeats()
    {
        const int seats = 20;
        const int requests = 200;
        using var secondInstance = new ApiFactory(db.ConnectionString);
        var clients = new[] { _factory.CreateClient(), secondInstance.CreateClient() };
        var schoolClass = await clients[0].CreateClassAsync(seats, "Two instances");

        var statuses = await FireSimultaneouslyAsync(requests, i =>
            clients[i % 2].ApplyAsync(schoolClass.Id, ApiClient.NewStudentId()));

        Assert.Equal(seats, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(requests - seats, statuses.Count(s => s == HttpStatusCode.Conflict));
        await AssertInvariantAsync(schoolClass.Id, expectedOccupied: seats);
    }

    [Fact]
    public async Task SimultaneousConfirmsCancelsAndApplications_DoNotDeadlockOrOverbook()
    {
        const int seats = 10;
        var client = _factory.CreateClient();
        var schoolClass = await client.CreateClassAsync(seats, "Mixed load");
        var holders = new List<(string StudentId, string HoldId)>();
        for (var i = 0; i < seats; i++)
        {
            var studentId = ApiClient.NewStudentId();
            holders.Add((studentId, (await client.ApplySuccessfullyAsync(schoolClass.Id, studentId)).HoldId));
        }

        var statuses = await FireSimultaneouslyAsync(60, i => i switch
        {
            < 5 => client.ConfirmAsync(holders[i].HoldId, holders[i].StudentId),
            < 10 => client.CancelAsync(holders[i].HoldId, holders[i].StudentId),
            _ => client.ApplyAsync(schoolClass.Id, ApiClient.NewStudentId())
        });

        Assert.All(statuses[..5], s => Assert.Equal(HttpStatusCode.OK, s));
        Assert.All(statuses[5..10], s => Assert.Equal(HttpStatusCode.NoContent, s));
        Assert.All(statuses[10..], s => Assert.Contains(s, new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }));

        var created = statuses[10..].Count(s => s == HttpStatusCode.Created);
        Assert.InRange(created, 0, 5);
        var (totalSeats, occupied, _) = await SeatInvariant.ReadAsync(db.ConnectionString, schoolClass.Id);
        Assert.True(occupied <= totalSeats, $"Seat invariant violated: {occupied} occupied > {totalSeats} total");
        Assert.Equal(5 + created, occupied);
    }

    [Fact]
    public async Task ConfirmDelayedPastExpiry_WhileAnotherStudentApplies_DoesNotOverbook()
    {
        using var shortHolds = new ApiFactory(db.ConnectionString, holdDurationSeconds: 3);
        var client = shortHolds.CreateClient();
        var schoolClass = await client.CreateClassAsync(totalSeats: 1, "Expiry boundary");
        var firstStudent = ApiClient.NewStudentId();
        var hold = await client.ApplySuccessfullyAsync(schoolClass.Id, firstStudent);

        // Holding the hold row's lock stalls the confirmation before its expiry check,
        // like a request that arrives just before the deadline but is delayed under load.
        await using var blocker = new NpgsqlConnection(db.ConnectionString);
        await blocker.OpenAsync();
        await using var blockerTransaction = await blocker.BeginTransactionAsync();
        await using (var lockHold = new NpgsqlCommand(
                         "SELECT 1 FROM admission_holds WHERE id = @id FOR UPDATE", blocker, blockerTransaction))
        {
            lockHold.Parameters.AddWithValue("id", Guid.Parse(hold.HoldId));
            await lockHold.ExecuteScalarAsync();
        }

        var confirmTask = client.ConfirmAsync(hold.HoldId, firstStudent);
        await WaitForLockWaitersAsync(1);

        var untilExpired = hold.ExpiresAt.ToUniversalTime() - DateTime.UtcNow + TimeSpan.FromMilliseconds(300);
        if (untilExpired > TimeSpan.Zero)
        {
            await Task.Delay(untilExpired);
        }

        var applyTask = client.ApplyAsync(schoolClass.Id, ApiClient.NewStudentId());
        await Task.WhenAny(applyTask, WaitForLockWaitersAsync(2));
        await blockerTransaction.RollbackAsync();

        using var confirm = await confirmTask;
        using var apply = await applyTask;

        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);
        Assert.Equal("HOLD_EXPIRED", (await confirm.ReadAsync<ErrorDto>()).Error);
        Assert.Equal(HttpStatusCode.Created, apply.StatusCode);

        var (totalSeats, occupied, _) = await SeatInvariant.ReadAsync(db.ConnectionString, schoolClass.Id);
        Assert.True(occupied <= totalSeats, $"Seat invariant violated: {occupied} occupied > {totalSeats} total");
        var stats = await client.GetClassAsync(schoolClass.Id);
        Assert.Equal((1, 0, 0), (stats.HeldSeats, stats.ConfirmedSeats, stats.AvailableSeats));
    }

    /// <summary>Waits until at least <paramref name="count"/> statements in the test database are blocked on a lock.</summary>
    private async Task WaitForLockWaitersAsync(int count)
    {
        await using var connection = new NpgsqlConnection(db.ConnectionString);
        await connection.OpenAsync();
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (true)
        {
            await using var command = new NpgsqlCommand(
                "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock'",
                connection);
            if ((long)(await command.ExecuteScalarAsync())! >= count)
            {
                return;
            }

            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Expected {count} statement(s) waiting on a lock.");
            }

            await Task.Delay(50);
        }
    }

    /// <summary>Starts all requests at the same moment and waits for every response.</summary>
    private static async Task<HttpStatusCode[]> FireSimultaneouslyAsync(
        int count, Func<int, Task<HttpResponseMessage>> send)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = Enumerable.Range(0, count).Select(async i =>
        {
            await start.Task;
            using var response = await send(i);
            return response.StatusCode;
        }).ToArray();

        start.SetResult();
        return await Task.WhenAll(requests);
    }

    private async Task AssertInvariantAsync(string classId, int expectedOccupied)
    {
        var (totalSeats, occupied, holdRows) = await SeatInvariant.ReadAsync(db.ConnectionString, classId);
        Assert.True(occupied <= totalSeats, $"Seat invariant violated: {occupied} occupied > {totalSeats} total");
        Assert.Equal(expectedOccupied, occupied);
        Assert.Equal(expectedOccupied, holdRows);
    }
}
