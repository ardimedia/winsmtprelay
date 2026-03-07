using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmtpServer;
using SmtpServer.ComponentModel;
using WinSmtpRelay.Core.Configuration;

namespace WinSmtpRelay.SmtpListener;

public class SmtpRelayServer : BackgroundService
{
    private readonly SmtpListenerOptions _config;
    private readonly RelayMessageStore _messageStore;
    private readonly RelayMailboxFilter _mailboxFilter;
    private readonly ILogger<SmtpRelayServer> _logger;

    public SmtpRelayServer(
        IOptions<SmtpListenerOptions> options,
        RelayMessageStore messageStore,
        RelayMailboxFilter mailboxFilter,
        ILogger<SmtpRelayServer> logger)
    {
        _config = options.Value;
        _messageStore = messageStore;
        _mailboxFilter = mailboxFilter;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var optionsBuilder = new SmtpServerOptionsBuilder()
            .ServerName("WinSmtpRelay")
            .MaxMessageSize(_config.MaxMessageSizeBytes);

        foreach (var endpoint in _config.Endpoints)
        {
            optionsBuilder.Endpoint(builder =>
            {
                builder.Port(endpoint.Port, endpoint.ImplicitTls);

                if (endpoint.RequireAuth)
                    builder.AllowUnsecureAuthentication(false);
            });

            _logger.LogInformation(
                "Configured SMTP endpoint on {Address}:{Port} (TLS={ImplicitTls}, Auth={RequireAuth})",
                endpoint.Address, endpoint.Port, endpoint.ImplicitTls, endpoint.RequireAuth);
        }

        var options = optionsBuilder.Build();

        var serviceProvider = new ServiceProvider();
        serviceProvider.Add(_messageStore);
        serviceProvider.Add(_mailboxFilter);

        var smtpServer = new SmtpServer.SmtpServer(options, serviceProvider);

        smtpServer.SessionCreated += (sender, args) =>
        {
            _logger.LogDebug("SMTP session created from {RemoteEndPoint}",
                args.Context.Properties.ContainsKey("RemoteEndPoint")
                    ? args.Context.Properties["RemoteEndPoint"]
                    : "unknown");
        };

        smtpServer.SessionCompleted += (sender, args) =>
        {
            _logger.LogDebug("SMTP session completed");
        };

        _logger.LogInformation("SMTP listener starting on {EndpointCount} endpoint(s)", _config.Endpoints.Count);

        try
        {
            await smtpServer.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("SMTP listener shutting down");
        }
    }
}
