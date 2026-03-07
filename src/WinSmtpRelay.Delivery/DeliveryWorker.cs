using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WinSmtpRelay.Core.Configuration;
using WinSmtpRelay.Core.Interfaces;
using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Delivery;

public class DeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<DeliveryOptions> options,
    IOptions<BackupMxOptions> backupMxOptions,
    ILogger<DeliveryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = options.Value;
        logger.LogInformation("Delivery worker starting with {MaxConcurrent} concurrent deliveries",
            config.MaxConcurrentDeliveries);

        using var semaphore = new SemaphoreSlim(config.MaxConcurrentDeliveries);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();

                var pending = await queue.GetPendingAsync(config.MaxConcurrentDeliveries, stoppingToken);

                if (pending.Count == 0)
                {
                    await Task.Delay(PollInterval, stoppingToken);
                    continue;
                }

                var tasks = pending.Select(async message =>
                {
                    await semaphore.WaitAsync(stoppingToken);
                    try
                    {
                        await ProcessMessageAsync(message, stoppingToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Delivery worker error");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        logger.LogInformation("Delivery worker shutting down");
    }

    private async Task ProcessMessageAsync(QueuedMessage message, CancellationToken cancellationToken)
    {
        var config = options.Value;

        using var scope = scopeFactory.CreateScope();
        var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
        var deliveryService = scope.ServiceProvider.GetRequiredService<IDeliveryService>();

        await queue.UpdateStatusAsync(message.Id, MessageStatus.Delivering, cancellationToken: cancellationToken);

        try
        {
            // Run message filters before delivery
            var filters = scope.ServiceProvider.GetServices<IMessageFilter>().OrderBy(f => f.Order);
            var filterContext = new MessageFilterContext
            {
                RawMessage = message.RawMessage,
                Sender = message.Sender,
                Recipients = message.Recipients,
                SourceIp = message.SourceIp
            };

            foreach (var filter in filters)
            {
                var result = await filter.FilterAsync(filterContext, cancellationToken);
                if (!result.Accept)
                {
                    await queue.UpdateStatusAsync(message.Id, MessageStatus.Bounced, $"Filtered: {result.RejectReason}", cancellationToken);
                    logger.LogInformation("Message {MessageId} rejected by filter: {Reason}",
                        message.MessageId, result.RejectReason);
                    return;
                }
                if (result.ModifiedRawMessage != null)
                {
                    filterContext.RawMessage = result.ModifiedRawMessage;
                    message.RawMessage = result.ModifiedRawMessage;
                    message.Sender = filterContext.Sender;
                }
            }

            await deliveryService.DeliverAsync(message, cancellationToken);
            await queue.UpdateStatusAsync(message.Id, MessageStatus.Delivered, cancellationToken: cancellationToken);

            logger.LogInformation("Message {MessageId} (id={QueueId}) delivered successfully",
                message.MessageId, message.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Delivery failed for message {MessageId} (id={QueueId}), attempt {Attempt}",
                message.MessageId, message.Id, message.RetryCount + 1);

            message.RetryCount++;
            message.LastError = ex.Message;

            // Use extended hold time for backup MX domains
            var effectiveConfig = config;
            var backupMx = backupMxOptions.Value;
            if (backupMx.Enabled && IsBackupMxMessage(message, backupMx))
            {
                effectiveConfig = new DeliveryOptions
                {
                    MaxRetryHours = backupMx.MaxHoldHours,
                    RetryIntervalsMinutes = [backupMx.RetryIntervalMinutes]
                };
            }

            var nextRetry = CalculateNextRetry(message.RetryCount, effectiveConfig);

            if (nextRetry == null || IsPermanentFailure(ex))
            {
                await queue.UpdateStatusAsync(message.Id, MessageStatus.Bounced, ex.Message, cancellationToken);
                logger.LogWarning("Message {MessageId} (id={QueueId}) bounced: {Error}",
                    message.MessageId, message.Id, ex.Message);
            }
            else
            {
                await queue.UpdateStatusAsync(message.Id, MessageStatus.Queued, ex.Message, cancellationToken);
                await queue.SetRetryAsync(message.Id, message.RetryCount, nextRetry.Value, cancellationToken);
            }
        }
    }

    internal static DateTime? CalculateNextRetry(int retryCount, DeliveryOptions config)
    {
        if (retryCount <= 0)
            return DateTime.UtcNow;

        var intervals = config.RetryIntervalsMinutes;
        if (intervals.Length == 0)
            return null;

        var intervalIndex = Math.Min(retryCount - 1, intervals.Length - 1);
        var delayMinutes = intervals[intervalIndex];

        // Check if total retry time exceeds max window
        // Sum configured intervals, then add repeated last interval for retries beyond the array
        var totalMinutes = intervals.Sum();
        if (retryCount > intervals.Length)
            totalMinutes += (retryCount - intervals.Length) * intervals[^1];
        if (totalMinutes > config.MaxRetryHours * 60)
            return null;

        return DateTime.UtcNow.AddMinutes(delayMinutes);
    }

    private static bool IsBackupMxMessage(QueuedMessage message, BackupMxOptions backupMx)
    {
        var recipientDomains = message.Recipients
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(r => r.Split('@').Last());

        return recipientDomains.Any(domain =>
            backupMx.Domains.Any(d => string.Equals(d, domain, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsPermanentFailure(Exception ex)
    {
        var message = ex.Message;
        return message.StartsWith("5") ||
               message.Contains("550") ||
               message.Contains("551") ||
               message.Contains("552") ||
               message.Contains("553") ||
               message.Contains("554");
    }
}
