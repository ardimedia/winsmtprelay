using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WinSmtpRelay.Core.Configuration;
using WinSmtpRelay.Core.Interfaces;

namespace WinSmtpRelay.Service;

public class StatisticsAggregator(
    IServiceScopeFactory scopeFactory,
    IOptions<StatisticsOptions> options,
    ILogger<StatisticsAggregator> logger) : BackgroundService
{
    private readonly StatisticsOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Backfill historical data on first run
        try
        {
            logger.LogInformation("Statistics aggregator starting — backfilling historical data");
            using (var scope = scopeFactory.CreateScope())
            {
                var stats = scope.ServiceProvider.GetRequiredService<IStatisticsService>();
                await stats.BackfillAsync(stoppingToken);
            }
            logger.LogInformation("Statistics backfill complete");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Statistics backfill failed");
        }

        // Daily aggregation loop
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayUntilNextRun();
            logger.LogInformation("Next statistics aggregation in {Delay}", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
                logger.LogInformation("Aggregating statistics for {Date}", yesterday);

                using var scope = scopeFactory.CreateScope();
                var stats = scope.ServiceProvider.GetRequiredService<IStatisticsService>();

                await stats.AggregateDayAsync(yesterday, stoppingToken);
                await stats.PurgeOldStatisticsAsync(_options.RetentionDays, stoppingToken);

                logger.LogInformation("Statistics aggregation complete for {Date}", yesterday);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Statistics aggregation failed");
            }
        }
    }

    private TimeSpan CalculateDelayUntilNextRun()
    {
        if (!TimeOnly.TryParse(_options.AggregationTimeUtc, out var targetTime))
            targetTime = new TimeOnly(0, 0);

        var now = DateTime.UtcNow;
        var todayTarget = now.Date.Add(targetTime.ToTimeSpan());

        if (todayTarget <= now)
            todayTarget = todayTarget.AddDays(1);

        return todayTarget - now;
    }
}
