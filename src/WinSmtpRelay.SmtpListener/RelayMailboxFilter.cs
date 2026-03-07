using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Storage;
using WinSmtpRelay.Core.Configuration;

namespace WinSmtpRelay.SmtpListener;

public class RelayMailboxFilter : MailboxFilter, IMailboxFilter
{
    private readonly SmtpListenerOptions _options;
    private readonly ILogger<RelayMailboxFilter> _logger;

    public RelayMailboxFilter(IOptions<SmtpListenerOptions> options, ILogger<RelayMailboxFilter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public override Task<bool> CanAcceptFromAsync(
        ISessionContext context,
        IMailbox from,
        int size,
        CancellationToken cancellationToken)
    {
        // Check message size limit
        if (size > 0 && size > _options.MaxMessageSizeBytes)
        {
            _logger.LogWarning("Message from {Sender} rejected: size {Size} exceeds limit {Limit}",
                from.AsAddress(), size, _options.MaxMessageSizeBytes);
            return Task.FromResult(false);
        }

        // Check IP-based relay restrictions
        if (context.Properties.TryGetValue("RemoteEndPoint", out var ep) && ep is IPEndPoint remoteEndPoint)
        {
            if (_options.AllowedNetworks.Count > 0 &&
                !IpNetworkHelper.IsInAnyNetwork(remoteEndPoint.Address, _options.AllowedNetworks))
            {
                _logger.LogWarning("Relay denied for {ClientIp}: not in allowed networks",
                    remoteEndPoint.Address);
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    public override Task<bool> CanDeliverToAsync(
        ISessionContext context,
        IMailbox to,
        IMailbox from,
        CancellationToken cancellationToken)
    {
        // If accepted domains are configured, check recipient domain
        if (_options.AcceptedDomains.Count > 0)
        {
            var recipientDomain = to.Host;
            var accepted = _options.AcceptedDomains.Any(d =>
                string.Equals(d, recipientDomain, StringComparison.OrdinalIgnoreCase));

            if (!accepted)
            {
                _logger.LogWarning("Recipient {Recipient} rejected: domain {Domain} not in accepted domains",
                    to.AsAddress(), recipientDomain);
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }
}
