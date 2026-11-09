using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SchoolAdmission.Tests.Infrastructure;

/// <summary>An in-process instance of the API connected to the shared test database.</summary>
public sealed class ApiFactory(string connectionString, int holdDurationSeconds = 600) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        builder.UseSetting("HOLD_DURATION_SECONDS", holdDurationSeconds.ToString());
    }
}
