using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using WinSmtpRelay.Core.Configuration;
using WinSmtpRelay.Core.Interfaces;
using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Delivery;

public class SmtpDeliveryService : IDeliveryService
{
    private readonly IMxResolver _mxResolver;
    private readonly DeliveryOptions _config;
    private readonly ILogger<SmtpDeliveryService> _logger;

    public SmtpDeliveryService(
        IMxResolver mxResolver,
        IOptions<DeliveryOptions> options,
        ILogger<SmtpDeliveryService> logger)
    {
        _mxResolver = mxResolver;
        _config = options.Value;
        _logger = logger;
    }

    public async Task DeliverAsync(QueuedMessage message, CancellationToken cancellationToken = default)
    {
        var mimeMessage = await MimeMessage.LoadAsync(new MemoryStream(message.RawMessage), cancellationToken);
        var recipients = message.Recipients.Split(';', StringSplitOptions.RemoveEmptyEntries);

        // Group recipients by domain for efficient delivery
        var byDomain = recipients.GroupBy(r => r.Split('@').Last(), StringComparer.OrdinalIgnoreCase);

        foreach (var domainGroup in byDomain)
        {
            var domain = domainGroup.Key;
            var domainRecipients = domainGroup.ToList();

            await DeliverToDomainAsync(mimeMessage, message.Sender, domainRecipients, domain, cancellationToken);
        }
    }

    private async Task DeliverToDomainAsync(
        MimeMessage mimeMessage,
        string sender,
        List<string> recipients,
        string domain,
        CancellationToken cancellationToken)
    {
        // Smart host takes priority if configured
        if (!string.IsNullOrWhiteSpace(_config.SmartHost))
        {
            await SendViaSmtpAsync(
                mimeMessage, sender, recipients,
                _config.SmartHost, _config.SmartHostPort,
                _config.SmartHostUsername, _config.SmartHostPassword,
                cancellationToken);
            return;
        }

        // Direct MX delivery
        var mxHosts = await _mxResolver.ResolveMxAsync(domain, cancellationToken);

        Exception? lastException = null;
        foreach (var mxHost in mxHosts)
        {
            try
            {
                await SendViaSmtpAsync(mimeMessage, sender, recipients, mxHost, 25, null, null, cancellationToken);
                return; // Success — stop trying other MX hosts
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Delivery to MX {MxHost} failed for domain {Domain}, trying next",
                    mxHost, domain);
            }
        }

        throw new InvalidOperationException(
            $"All MX hosts exhausted for domain {domain}", lastException);
    }

    private async Task SendViaSmtpAsync(
        MimeMessage mimeMessage,
        string sender,
        List<string> recipients,
        string host,
        int port,
        string? username,
        string? password,
        CancellationToken cancellationToken)
    {
        using var client = new SmtpClient(new MailKitProtocolLogger(_logger));

        var tlsOption = _config.OpportunisticTls
            ? SecureSocketOptions.StartTlsWhenAvailable
            : SecureSocketOptions.None;

        _logger.LogDebug("Connecting to {Host}:{Port} (TLS={TlsOption})", host, port, tlsOption);

        await client.ConnectAsync(host, port, tlsOption, cancellationToken);

        if (!string.IsNullOrWhiteSpace(username))
            await client.AuthenticateAsync(username, password, cancellationToken);

        var senderAddress = MailboxAddress.Parse(sender);
        var recipientAddresses = recipients.Select(MailboxAddress.Parse).ToList();

        await client.SendAsync(mimeMessage, senderAddress, recipientAddresses, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("Delivered to {Recipients} via {Host}:{Port}",
            string.Join(", ", recipients), host, port);
    }
}
