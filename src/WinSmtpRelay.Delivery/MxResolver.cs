using DnsClient;
using DnsClient.Protocol;
using Microsoft.Extensions.Logging;
using WinSmtpRelay.Core.Interfaces;

namespace WinSmtpRelay.Delivery;

public class MxResolver : IMxResolver
{
    private readonly ILookupClient _dns;
    private readonly ILogger<MxResolver> _logger;

    public MxResolver(ILookupClient dns, ILogger<MxResolver> logger)
    {
        _dns = dns;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> ResolveMxAsync(string domain, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _dns.QueryAsync(domain, QueryType.MX, cancellationToken: cancellationToken);

            var mxRecords = result.Answers
                .OfType<MxRecord>()
                .OrderBy(mx => mx.Preference)
                .Select(mx => mx.Exchange.Value.TrimEnd('.'))
                .Where(host => !string.IsNullOrWhiteSpace(host))
                .ToList();

            if (mxRecords.Count > 0)
            {
                _logger.LogDebug("MX for {Domain}: {MxRecords}", domain, string.Join(", ", mxRecords));
                return mxRecords;
            }

            // RFC 5321 §5.1: If no MX records, fall back to A/AAAA record (use domain as host)
            _logger.LogDebug("No MX for {Domain}, falling back to A record", domain);
            return [domain];
        }
        catch (DnsResponseException ex)
        {
            _logger.LogWarning(ex, "DNS MX lookup failed for {Domain}", domain);
            return [domain];
        }
    }
}
