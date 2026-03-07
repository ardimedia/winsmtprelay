using System.Net;
using System.Net.Sockets;
using DnsClient;
using DnsClient.Protocol;
using Microsoft.Extensions.Logging;
using WinSmtpRelay.Security.Models;

namespace WinSmtpRelay.Security;

public class SpfValidator
{
    private readonly ILookupClient _dns;
    private readonly ILogger<SpfValidator> _logger;
    private const int MaxDnsLookups = 10; // RFC 7208 §4.6.4

    public SpfValidator(ILookupClient dns, ILogger<SpfValidator> logger)
    {
        _dns = dns;
        _logger = logger;
    }

    public async Task<SpfCheckResult> CheckAsync(
        IPAddress senderIp,
        string mailFromDomain,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mailFromDomain))
            return new SpfCheckResult(SpfVerdict.None, "no domain");

        try
        {
            return await EvaluateAsync(senderIp, mailFromDomain, 0, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "SPF check error for {Domain}", mailFromDomain);
            return new SpfCheckResult(SpfVerdict.TempError, $"error: {ex.Message}");
        }
    }

    private async Task<SpfCheckResult> EvaluateAsync(
        IPAddress senderIp,
        string domain,
        int lookupCount,
        CancellationToken cancellationToken)
    {
        if (lookupCount >= MaxDnsLookups)
            return new SpfCheckResult(SpfVerdict.PermError, "too many DNS lookups");

        var spfRecord = await GetSpfRecordAsync(domain, cancellationToken);
        if (spfRecord is null)
            return new SpfCheckResult(SpfVerdict.None, $"no SPF record for {domain}");

        var senderIpv4 = senderIp.IsIPv4MappedToIPv6 ? senderIp.MapToIPv4() : senderIp;

        var terms = spfRecord.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLookups = lookupCount;

        foreach (var term in terms.Skip(1)) // skip "v=spf1"
        {
            // Handle redirect modifier
            if (term.StartsWith("redirect=", StringComparison.OrdinalIgnoreCase))
            {
                var redirectDomain = term[9..];
                currentLookups++;
                return await EvaluateAsync(senderIpv4, redirectDomain, currentLookups, cancellationToken);
            }

            // Parse qualifier
            var qualifier = term[0] switch
            {
                '+' => SpfVerdict.Pass,
                '-' => SpfVerdict.Fail,
                '~' => SpfVerdict.SoftFail,
                '?' => SpfVerdict.Neutral,
                _ => SpfVerdict.Pass // default qualifier is +
            };
            var mechanism = term.TrimStart('+', '-', '~', '?');

            // Evaluate mechanism
            if (mechanism.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return new SpfCheckResult(qualifier, $"{term} matched");
            }

            if (mechanism.StartsWith("ip4:", StringComparison.OrdinalIgnoreCase))
            {
                var cidr = mechanism[4..];
                if (senderIpv4.AddressFamily == AddressFamily.InterNetwork && IsInCidr(senderIpv4, cidr))
                    return new SpfCheckResult(qualifier, $"{term} matched {senderIpv4}");
            }
            else if (mechanism.StartsWith("ip6:", StringComparison.OrdinalIgnoreCase))
            {
                var cidr = mechanism[4..];
                if (senderIp.AddressFamily == AddressFamily.InterNetworkV6 && IsInCidr(senderIp, cidr))
                    return new SpfCheckResult(qualifier, $"{term} matched {senderIp}");
            }
            else if (mechanism.StartsWith("include:", StringComparison.OrdinalIgnoreCase))
            {
                var includeDomain = mechanism[8..];
                currentLookups++;
                var includeResult = await EvaluateAsync(senderIpv4, includeDomain, currentLookups, cancellationToken);
                if (includeResult.Verdict == SpfVerdict.Pass)
                    return new SpfCheckResult(qualifier, $"include:{includeDomain} passed");
            }
            else if (mechanism.Equals("a", StringComparison.OrdinalIgnoreCase) ||
                     mechanism.StartsWith("a:", StringComparison.OrdinalIgnoreCase) ||
                     mechanism.StartsWith("a/", StringComparison.OrdinalIgnoreCase))
            {
                currentLookups++;
                var (aDomain, prefixLen) = ParseDomainAndPrefix(mechanism, domain, "a");
                if (await MatchesARecordAsync(senderIpv4, aDomain, prefixLen, cancellationToken))
                    return new SpfCheckResult(qualifier, $"{term} matched {senderIpv4}");
            }
            else if (mechanism.Equals("mx", StringComparison.OrdinalIgnoreCase) ||
                     mechanism.StartsWith("mx:", StringComparison.OrdinalIgnoreCase) ||
                     mechanism.StartsWith("mx/", StringComparison.OrdinalIgnoreCase))
            {
                currentLookups++;
                var (mxDomain, prefixLen) = ParseDomainAndPrefix(mechanism, domain, "mx");
                if (await MatchesMxRecordAsync(senderIpv4, mxDomain, prefixLen, currentLookups, cancellationToken))
                    return new SpfCheckResult(qualifier, $"{term} matched {senderIpv4}");
            }
        }

        return new SpfCheckResult(SpfVerdict.Neutral, "no mechanism matched");
    }

    private async Task<string?> GetSpfRecordAsync(string domain, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dns.QueryAsync(domain, QueryType.TXT, cancellationToken: cancellationToken);
            return result.Answers
                .OfType<TxtRecord>()
                .Select(txt => string.Join("", txt.Text))
                .FirstOrDefault(t => t.StartsWith("v=spf1 ", StringComparison.OrdinalIgnoreCase) ||
                                     t.Equals("v=spf1", StringComparison.OrdinalIgnoreCase));
        }
        catch (DnsResponseException)
        {
            return null;
        }
    }

    private async Task<bool> MatchesARecordAsync(
        IPAddress senderIp, string domain, int prefixLen, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dns.QueryAsync(domain, QueryType.A, cancellationToken: cancellationToken);
            foreach (var a in result.Answers.OfType<ARecord>())
            {
                if (prefixLen == 32 ? a.Address.Equals(senderIp) : IsInCidr(senderIp, $"{a.Address}/{prefixLen}"))
                    return true;
            }
        }
        catch (DnsResponseException) { }
        return false;
    }

    private async Task<bool> MatchesMxRecordAsync(
        IPAddress senderIp, string domain, int prefixLen, int lookupCount, CancellationToken cancellationToken)
    {
        try
        {
            var mxResult = await _dns.QueryAsync(domain, QueryType.MX, cancellationToken: cancellationToken);
            var currentLookups = lookupCount;
            foreach (var mx in mxResult.Answers.OfType<MxRecord>())
            {
                currentLookups++;
                if (currentLookups >= MaxDnsLookups) break;
                var mxHost = mx.Exchange.Value.TrimEnd('.');
                if (await MatchesARecordAsync(senderIp, mxHost, prefixLen, cancellationToken))
                    return true;
            }
        }
        catch (DnsResponseException) { }
        return false;
    }

    private static (string domain, int prefixLen) ParseDomainAndPrefix(string mechanism, string defaultDomain, string prefix)
    {
        var rest = mechanism.Length > prefix.Length ? mechanism[prefix.Length..] : "";
        var domain = defaultDomain;
        var prefixLen = 32;

        if (rest.StartsWith(':'))
        {
            var slashIdx = rest.IndexOf('/');
            if (slashIdx > 0)
            {
                domain = rest[1..slashIdx];
                int.TryParse(rest[(slashIdx + 1)..], out prefixLen);
            }
            else
            {
                domain = rest[1..];
            }
        }
        else if (rest.StartsWith('/'))
        {
            int.TryParse(rest[1..], out prefixLen);
        }

        return (domain, prefixLen);
    }

    private static bool IsInCidr(IPAddress address, string cidr)
    {
        var parts = cidr.Split('/');
        if (!IPAddress.TryParse(parts[0], out var network))
            return false;

        if (parts.Length == 1)
            return address.Equals(network);

        if (!int.TryParse(parts[1], out var prefixLength))
            return false;

        var addressBytes = address.GetAddressBytes();
        var networkBytes = network.GetAddressBytes();
        if (addressBytes.Length != networkBytes.Length)
            return false;

        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (var i = 0; i < fullBytes && i < addressBytes.Length; i++)
        {
            if (addressBytes[i] != networkBytes[i])
                return false;
        }

        if (remainingBits > 0 && fullBytes < addressBytes.Length)
        {
            var mask = (byte)(0xFF << (8 - remainingBits));
            if ((addressBytes[fullBytes] & mask) != (networkBytes[fullBytes] & mask))
                return false;
        }

        return true;
    }
}
