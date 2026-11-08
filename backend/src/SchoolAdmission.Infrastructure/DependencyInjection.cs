using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Infrastructure.Persistence;

namespace SchoolAdmission.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string is not configured. Set ConnectionStrings__DefaultConnection.");
        }

        services.AddDbContext<AppDbContext>(options => options.UseAppDatabase(connectionString));
        services.AddSingleton(TimeProvider.System);

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
}
