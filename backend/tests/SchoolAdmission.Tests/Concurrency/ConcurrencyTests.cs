using System.Net;
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
