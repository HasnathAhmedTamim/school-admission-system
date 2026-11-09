using System.Net;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Api;

[Collection(PostgresCollection.Name)]
public sealed class HoldsApiTests(PostgresFixture db) : IDisposable
{
    private readonly ApiFactory _factory = new(db.ConnectionString, holdDurationSeconds: 600);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Apply_Returns201_WithHeldHoldMatchingContract()
    {
        var client = _factory.CreateClient();
        var schoolClass = await client.CreateClassAsync(totalSeats: 5);
        var studentId = ApiClient.NewStudentId();
        var before = DateTime.UtcNow;

        var response = await client.ApplyAsync(schoolClass.Id, studentId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var hold = await response.ReadAsync<HoldDto>();
        Assert.False(string.IsNullOrWhiteSpace(hold.HoldId));
        Assert.Equal(schoolClass.Id, hold.ClassId);
        Assert.Equal(studentId, hold.StudentId);
        Assert.Equal("HELD", hold.Status);
        Assert.InRange(hold.ExpiresAt, before.AddSeconds(595), before.AddSeconds(605));

        var seats = await client.GetClassAsync(schoolClass.Id);
        Assert.Equal((1, 0, 4), (seats.HeldSeats, seats.ConfirmedSeats, seats.AvailableSeats));
    }

    [Fact]
    public async Task Apply_WhenClassIsFull_Returns409()
    {
        var client = _factory.CreateClient();
        var schoolClass = await client.CreateClassAsync(totalSeats: 2);
        await client.ApplySuccessfullyAsync(schoolClass.Id, ApiClient.NewStudentId());
        await client.ApplySuccessfullyAsync(schoolClass.Id, ApiClient.NewStudentId());

        var response = await client.ApplyAsync(schoolClass.Id, ApiClient.NewStudentId());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("NO_SEATS_AVAILABLE", (await response.ReadAsync<ErrorDto>()).Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("2015269123456789")]
    [InlineData("201526912345678901")]
    [InlineData("2015269123456789X")]
    public async Task Apply_MissingOrInvalidStudentId_Returns400(string? studentId)
    {
        var client = _factory.CreateClient();
        var schoolClass = await client.CreateClassAsync(totalSeats: 5);

        var response = await client.ApplyAsync(schoolClass.Id, studentId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("0193f6a1-0000-7000-8000-000000000000")]
    [InlineData("c_123")]
    public async Task Apply_UnknownClass_Returns404(string classId)
    {
        var client = _factory.CreateClient();

        var response = await client.ApplyAsync(classId, ApiClient.NewStudentId());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Apply_SameStudentTwiceForSameClass_Returns409()
    {
        var client = _factory.CreateClient();
        var schoolClass = await client.CreateClassAsync(totalSeats: 5);
        var studentId = ApiClient.NewStudentId();
        await client.ApplySuccessfullyAsync(schoolClass.Id, studentId);

        var response = await client.ApplyAsync(schoolClass.Id, studentId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("ALREADY_APPLIED", (await response.ReadAsync<ErrorDto>()).Error);
    }

    [Fact]
    public async Task Confirm_IsIdempotent_AndCountsSeatOnce()
    {
        var client = _factory.CreateClient();
        var schoolClass = await client.CreateClassAsync(totalSeats: 3);
        var studentId = ApiClient.NewStudentId();
        var hold = await client.ApplySuccessfullyAsync(schoolClass.Id, studentId);

        var first = await client.ConfirmAsync(hold.HoldId, studentId);
        var second = await client.ConfirmAsync(hold.HoldId, studentId);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var confirmed = await second.ReadAsync<HoldDto>();
        Assert.Equal(hold.HoldId, confirmed.HoldId);
        Assert.Equal("CONFIRMED", confirmed.Status);

        var seats = await client.GetClassAsync(schoolClass.Id);
        Assert.Equal((0, 1, 2), (seats.HeldSeats, seats.ConfirmedSeats, seats.AvailableSeats));

        var reapply = await client.ApplyAsync(schoolClass.Id, studentId);
        Assert.Equal(HttpStatusCode.Conflict, reapply.StatusCode);
    }

    [Fact]
    public async Task OtherStudent_CannotViewConfirmOrCancel_Returns403()
    {
        var client = _factory.CreateClient();
        var schoolClass = await client.CreateClassAsync(totalSeats: 3);
        var owner = ApiClient.NewStudentId();
        var intruder = ApiClient.NewStudentId();
        var hold = await client.ApplySuccessfullyAsync(schoolClass.Id, owner);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetHoldAsync(hold.HoldId, intruder)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.ConfirmAsync(hold.HoldId, intruder)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.CancelAsync(hold.HoldId, intruder)).StatusCode);

        var status = await (await client.GetHoldAsync(hold.HoldId, owner)).ReadAsync<HoldDto>();
        Assert.Equal("HELD", status.Status);
    }

    [Fact]
    public async Task ConfirmAndCancel_WithoutStudentId_Return400()
    {
        var client = _factory.CreateClient();
        var schoolClass = await client.CreateClassAsync(totalSeats: 3);
        var hold = await client.ApplySuccessfullyAsync(schoolClass.Id, ApiClient.NewStudentId());

        Assert.Equal(HttpStatusCode.BadRequest, (await client.ConfirmAsync(hold.HoldId, null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.CancelAsync(hold.HoldId, null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetMyHoldsAsync(null)).StatusCode);
    }

    [Theory]
    [InlineData("0193f6a1-0000-7000-8000-000000000000")]
    [InlineData("h_789")]
    public async Task UnknownOrMalformedHoldId_Returns404(string holdId)
    {
        var client = _factory.CreateClient();
        var studentId = ApiClient.NewStudentId();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetHoldAsync(holdId)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.ConfirmAsync(holdId, studentId)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.CancelAsync(holdId, studentId)).StatusCode);
    }

    [Fact]
    public async Task Cancel_ReleasesSeat_AndCancelledHoldCannotBeConfirmed()
    {
        var client = _factory.CreateClient();
        var schoolClass = await client.CreateClassAsync(totalSeats: 1);
        var studentId = ApiClient.NewStudentId();
        var hold = await client.ApplySuccessfullyAsync(schoolClass.Id, studentId);
        Assert.Equal(HttpStatusCode.Conflict, (await client.ApplyAsync(schoolClass.Id, ApiClient.NewStudentId())).StatusCode);

        var cancel = await client.CancelAsync(hold.HoldId, studentId);

        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.CancelAsync(hold.HoldId, studentId)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.ConfirmAsync(hold.HoldId, studentId)).StatusCode);
        Assert.Equal(1, (await client.GetClassAsync(schoolClass.Id)).AvailableSeats);
        await client.ApplySuccessfullyAsync(schoolClass.Id, ApiClient.NewStudentId());
    }

    [Fact]
    public async Task Cancel_ConfirmedAdmission_Returns409()
    {
        var client = _factory.CreateClient();
        var schoolClass = await client.CreateClassAsync(totalSeats: 1);
        var studentId = ApiClient.NewStudentId();
        var hold = await client.ApplySuccessfullyAsync(schoolClass.Id, studentId);
        await client.ConfirmAsync(hold.HoldId, studentId);

        var response = await client.CancelAsync(hold.HoldId, studentId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, (await client.GetClassAsync(schoolClass.Id)).ConfirmedSeats);
    }

    [Fact]
    public async Task MyHolds_ReturnsOnlyThisStudentsApplications_WithClassName()
    {
        var client = _factory.CreateClient();
        var first = await client.CreateClassAsync(totalSeats: 2, prefix: "Class 6 - Morning");
        var second = await client.CreateClassAsync(totalSeats: 2, prefix: "Class 6 - Day");
        var studentId = ApiClient.NewStudentId();
        await client.ApplySuccessfullyAsync(first.Id, studentId);
        await client.ApplySuccessfullyAsync(second.Id, studentId);
        await client.ApplySuccessfullyAsync(first.Id, ApiClient.NewStudentId());

        var response = await client.GetMyHoldsAsync(studentId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var holds = await response.ReadAsync<List<HoldDto>>();
        Assert.Equal(2, holds.Count);
        Assert.Contains(holds, h => h.ClassId == first.Id && h.ClassName == first.Name && h.Status == "HELD");
        Assert.Contains(holds, h => h.ClassId == second.Id && h.ClassName == second.Name && h.Status == "HELD");
    }
}
