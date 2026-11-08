using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Application.Classes;
using SchoolAdmission.Infrastructure.Persistence;
using SchoolAdmission.Infrastructure.Persistence.Repositories;

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

        services.AddScoped<IClassRepository, ClassRepository>();

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
