using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SchoolAdmission.Infrastructure.Persistence;

public static class DatabaseMigrator
{
    /// <summary>
    /// Applies pending EF Core migrations, retrying while the database is still starting up.
    /// EF Core 9 takes a database-wide migration lock, so concurrent instances are safe.
    /// </summary>
    public static async Task MigrateDatabaseAsync(
        this IServiceProvider services,
        ILogger logger,
        int maxAttempts = 10,
        TimeSpan? retryDelay = null,
        CancellationToken cancellationToken = default)
    {
        var delay = retryDelay ?? TimeSpan.FromSeconds(3);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Database migrations applied");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex,
                    "Database migration attempt {Attempt}/{MaxAttempts} failed; retrying in {Delay}s",
                    attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
