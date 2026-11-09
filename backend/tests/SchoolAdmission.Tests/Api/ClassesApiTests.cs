using System.Net;
using System.Net.Http.Json;
using System.Text;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Api;

[Collection(PostgresCollection.Name)]
public sealed class ClassesApiTests(PostgresFixture db) : IDisposable
{
    private readonly ApiFactory _factory = new(db.ConnectionString);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task CreateClass_Returns201_WithSeatStatistics()
    {
        var client = _factory.CreateClient();
        var name = ApiClient.UniqueName("Class 6 - Morning Shift");

        var response = await client.PostAsJsonAsync("/classes", new { name, totalSeats = 120 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.ReadAsync<ClassDto>();
        Assert.False(string.IsNullOrWhiteSpace(created.Id));
        Assert.Equal(name, created.Name);
        Assert.Equal(120, created.TotalSeats);
        Assert.Equal(0, created.HeldSeats);
        Assert.Equal(0, created.ConfirmedSeats);
        Assert.Equal(120, created.AvailableSeats);
        Assert.Equal($"/classes/{created.Id}", response.Headers.Location?.AbsolutePath);

        var listed = await client.GetFromJsonAsync<List<ClassDto>>("/classes", ApiClient.Json);
        Assert.Contains(listed!, c => c.Id == created.Id && c.AvailableSeats == 120);
    }

    [Theory]
    [InlineData("""{ "totalSeats": 10 }""")]
    [InlineData("""{ "name": "   ", "totalSeats": 10 }""")]
    [InlineData("""{ "name": "Class 7" }""")]
    [InlineData("""{ "name": "Class 7", "totalSeats": 0 }""")]
    [InlineData("""{ "name": "Class 7", "totalSeats": -1 }""")]
    [InlineData("""{ "name": "Class 7", "totalSeats": 12.5 }""")]
    [InlineData("""{ "name": "Class 7", "totalSeats": "many" }""")]
    [InlineData("""{ "name": """)]
    [InlineData("")]
    public async Task CreateClass_InvalidInput_Returns400(string body)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/classes", new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.ReadAsync<ErrorDto>();
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact]
    public async Task CreateClass_DuplicateNameIgnoringCase_Returns409()
    {
        var client = _factory.CreateClient();
        var name = ApiClient.UniqueName("Class 8 - Day Shift");
        await client.PostAsJsonAsync("/classes", new { name, totalSeats = 10 });

        var response = await client.PostAsJsonAsync("/classes", new { name = name.ToUpperInvariant(), totalSeats = 10 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("0193f6a1-0000-7000-8000-000000000000")]
    [InlineData("c_123")]
    public async Task GetClass_UnknownOrMalformedId_Returns404(string classId)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/classes/{classId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
