using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SchoolAdmission.Tests.Infrastructure;

public sealed record ClassDto(string Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats);

public sealed record HoldDto(
    string HoldId, string ClassId, string? ClassName, string? StudentId, string Status, DateTime ExpiresAt);

public sealed record ErrorDto(string Error, string Message);

/// <summary>Thin helpers over HttpClient matching the public API contract.</summary>
public static class ApiClient
{
    private const string StudentHeader = "X-Student-Id";
    private static long _studentCounter = Random.Shared.NextInt64(10_000_000_000_000_000, 90_000_000_000_000_000);

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Returns a unique, valid 17-digit birth registration number.</summary>
    public static string NewStudentId() => Interlocked.Increment(ref _studentCounter).ToString();

    public static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}";

    public static async Task<ClassDto> CreateClassAsync(this HttpClient client, int totalSeats, string prefix = "Class")
    {
        var response = await client.PostAsJsonAsync("/classes", new { name = UniqueName(prefix), totalSeats });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ClassDto>(Json))!;
    }

    public static async Task<ClassDto> GetClassAsync(this HttpClient client, string classId) =>
        (await client.GetFromJsonAsync<ClassDto>($"/classes/{classId}", Json))!;

    public static Task<HttpResponseMessage> ApplyAsync(this HttpClient client, string classId, string? studentId) =>
        client.SendAsync(WithStudent(HttpMethod.Post, $"/classes/{classId}/holds", studentId));

    public static async Task<HoldDto> ApplySuccessfullyAsync(this HttpClient client, string classId, string studentId)
    {
        var response = await client.ApplyAsync(classId, studentId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<HoldDto>(Json))!;
    }

    public static Task<HttpResponseMessage> GetHoldAsync(this HttpClient client, string holdId, string? studentId = null) =>
        client.SendAsync(WithStudent(HttpMethod.Get, $"/holds/{holdId}", studentId));

    public static Task<HttpResponseMessage> ConfirmAsync(this HttpClient client, string holdId, string? studentId) =>
        client.SendAsync(WithStudent(HttpMethod.Post, $"/holds/{holdId}/confirm", studentId));

    public static Task<HttpResponseMessage> CancelAsync(this HttpClient client, string holdId, string? studentId) =>
        client.SendAsync(WithStudent(HttpMethod.Delete, $"/holds/{holdId}", studentId));

    public static Task<HttpResponseMessage> GetMyHoldsAsync(this HttpClient client, string? studentId) =>
        client.SendAsync(WithStudent(HttpMethod.Get, "/me/holds", studentId));

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    private static HttpRequestMessage WithStudent(HttpMethod method, string path, string? studentId)
    {
        var request = new HttpRequestMessage(method, path);
        if (studentId is not null)
        {
            request.Headers.TryAddWithoutValidation(StudentHeader, studentId);
        }

        return request;
    }
}
