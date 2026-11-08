using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SchoolAdmission.Application.Admissions;
using SchoolAdmission.Application.Classes;
using SchoolAdmission.Infrastructure.Persistence;
using SchoolAdmission.Infrastructure.Persistence.Repositories;

namespace SchoolAdmission.Infrastructure;

public static class DependencyInjection
{
    // Keeps several API instances well below PostgreSQL's default max_connections (100).
    private const int DefaultMaxPoolSize = 30;
    private const int NpgsqlDefaultMaxPoolSize = 100;

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string is not configured. Set ConnectionStrings__DefaultConnection.");
        }

        var effectiveConnectionString = ApplyPoolDefaults(connectionString);

        services.AddDbContext<AppDbContext>(options => options.UseAppDatabase(effectiveConnectionString));
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IClassRepository, ClassRepository>();
        services.AddScoped<IAdmissionHoldRepository, AdmissionHoldRepository>();

        return services;
    }

    internal static TBuilder UseAppDatabase<TBuilder>(this TBuilder options, string connectionString)
        where TBuilder : DbContextOptionsBuilder
    {
        options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();
        return options;
    }

    private static string ApplyPoolDefaults(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (builder.MaxPoolSize == NpgsqlDefaultMaxPoolSize)
        {
            builder.MaxPoolSize = DefaultMaxPoolSize;
        }

        return builder.ConnectionString;
    }
}
