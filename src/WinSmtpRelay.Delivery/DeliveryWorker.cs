using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WinSmtpRelay.Core.Configuration;
using WinSmtpRelay.Core.Interfaces;

namespace WinSmtpRelay.Delivery;

/// <summary>
/// Background service that polls the message queue and delivers messages.
/// </summary>
public class DeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<DeliveryOptions> options,
    ILogger<DeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = options.Value;
        logger.LogInformation("Delivery worker starting with {MaxConcurrent} concurrent deliveries",
            config.MaxConcurrentDeliveries);

        // TODO: Phase 1 — Poll queue, deliver via MailKit, handle retries + bounces

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
