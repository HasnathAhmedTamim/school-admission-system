using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolAdmission.Infrastructure.Persistence;

namespace SchoolAdmission.Infrastructure.BackgroundJobs;

/// <summary>
/// Periodically persists EXPIRED for HELD holds whose time limit has passed.
/// Seat counts never depend on this worker (they exclude past-due holds directly);
/// it keeps stored statuses accurate. Safe to run in several API instances at once.
/// </summary>
public sealed class HoldExpiryWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    ILogger<HoldExpiryWorker> logger) : BackgroundService
{
    public static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 500;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SweepInterval, clock);

        do
        {
            try
            {
                var expired = await ExpireDueHoldsAsync(stoppingToken);
                if (expired > 0)
                {
                    logger.LogInformation("Marked {Count} overdue holds as EXPIRED", expired);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Hold expiry sweep failed; retrying in {Seconds}s", SweepInterval.TotalSeconds);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task<int> ExpireDueHoldsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = clock.GetUtcNow().UtcDateTime;

        var total = 0;
        int affected;
        do
        {
            // SKIP LOCKED: rows being confirmed/cancelled, or swept by another instance, are left alone
            // instead of blocking; they are picked up on a later sweep if still due.
            affected = await db.Database.ExecuteSqlAsync(
                $"""
                UPDATE admission_holds
                SET status = 'EXPIRED'
                WHERE status = 'HELD'
                  AND id IN (
                      SELECT id FROM admission_holds
                      WHERE status = 'HELD' AND expires_at <= {now}
                      ORDER BY expires_at
                      LIMIT {BatchSize}
                      FOR UPDATE SKIP LOCKED)
                """,
                cancellationToken);
            total += affected;
        }
        while (affected == BatchSize && !cancellationToken.IsCancellationRequested);

        return total;
    }
}
