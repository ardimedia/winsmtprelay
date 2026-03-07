using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WinSmtpRelay.Core.Configuration;

namespace WinSmtpRelay.SmtpListener;

/// <summary>
/// Hosted service that runs the SMTP listener using SmtpServer NuGet.
/// </summary>
public class SmtpRelayServer(
    IOptions<SmtpListenerOptions> options,
    ILogger<SmtpRelayServer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = options.Value;
        logger.LogInformation("SMTP listener starting on {EndpointCount} endpoint(s)", config.Endpoints.Count);

        // TODO: Phase 1 — Initialize SmtpServer NuGet with configured endpoints
        // TODO: Phase 1 — Wire up IMessageStore and IMailboxFilter

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
