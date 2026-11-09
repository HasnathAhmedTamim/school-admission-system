using System.Net;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Api;

[Collection(PostgresCollection.Name)]
public sealed class PersistenceTests(PostgresFixture db)
{
    [Fact]
    public async Task DataSurvivesApplicationRestart()
    {
        string classId, holdId, studentId = ApiClient.NewStudentId();

        using (var firstRun = new ApiFactory(db.ConnectionString))
        {
            var client = firstRun.CreateClient();
            var schoolClass = await client.CreateClassAsync(totalSeats: 4);
            var hold = await client.ApplySuccessfullyAsync(schoolClass.Id, studentId);
            await client.ConfirmAsync(hold.HoldId, studentId);
            (classId, holdId) = (schoolClass.Id, hold.HoldId);
        }

        using var secondRun = new ApiFactory(db.ConnectionString);
        var restarted = secondRun.CreateClient();

        var seats = await restarted.GetClassAsync(classId);
        Assert.Equal((0, 1, 3), (seats.HeldSeats, seats.ConfirmedSeats, seats.AvailableSeats));

        var response = await restarted.GetHoldAsync(holdId, studentId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("CONFIRMED", (await response.ReadAsync<HoldDto>()).Status);
    }
}
