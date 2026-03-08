using DnsClient;
using DnsClient.Protocol;
using Microsoft.Extensions.Logging;
using Nager.EmailAuthentication;
using Nager.EmailAuthentication.Models.Dmarc;
using WinSmtpRelay.Security.Models;
using DmarcPolicyLocal = WinSmtpRelay.Security.Models.DmarcPolicy;
using NagerDmarcPolicy = Nager.EmailAuthentication.Models.Dmarc.DmarcPolicy;

namespace WinSmtpRelay.Security;

public class DmarcValidator
{
    private readonly ILookupClient _dns;
    private readonly ILogger<DmarcValidator> _logger;

    public DmarcValidator(ILookupClient dns, ILogger<DmarcValidator> logger)
    {
        _dns = dns;
        _logger = logger;
    }

    public async Task<DmarcCheckResult> CheckAsync(
        string headerFromDomain,
        string envelopeFromDomain,
        SpfCheckResult spfResult,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(headerFromDomain))
            return new DmarcCheckResult(DmarcVerdict.None, DmarcPolicyLocal.None, "no From domain");

        try
        {
            var dmarcRaw = await GetDmarcRecordAsync(headerFromDomain, cancellationToken);
            if (dmarcRaw is null)
            {
                var orgDomain = GetOrganizationalDomain(headerFromDomain);
                if (orgDomain != headerFromDomain)
                    dmarcRaw = await GetDmarcRecordAsync(orgDomain, cancellationToken);
            }

            if (dmarcRaw is null)
                return new DmarcCheckResult(DmarcVerdict.None, DmarcPolicyLocal.None, $"no DMARC record for {headerFromDomain}");

            if (!DmarcRecordParser.TryParse(dmarcRaw, out var dmarcRecordBase) || dmarcRecordBase is not DmarcRecordV1 dmarcRecord)
                return new DmarcCheckResult(DmarcVerdict.PermError, DmarcPolicyLocal.None, "invalid DMARC record");

            var policy = dmarcRecord.DomainPolicy switch
            {
                NagerDmarcPolicy.Reject => DmarcPolicyLocal.Reject,
                NagerDmarcPolicy.Quarantine => DmarcPolicyLocal.Quarantine,
                _ => DmarcPolicyLocal.None
            };

            // DMARC passes if SPF passes AND the domains are aligned
            var strictSpf = dmarcRecord.SpfAlignmentMode == AlignmentMode.Strict;
            var spfAligned = spfResult.Verdict == SpfVerdict.Pass &&
                             AreDomainAligned(headerFromDomain, envelopeFromDomain, strictSpf);

            if (spfAligned)
                return new DmarcCheckResult(DmarcVerdict.Pass, policy, $"SPF aligned for {headerFromDomain}");

            return new DmarcCheckResult(DmarcVerdict.Fail, policy,
                $"SPF not aligned for {headerFromDomain} (spf={spfResult.Verdict}, envelope={envelopeFromDomain})");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "DMARC check error for {Domain}", headerFromDomain);
            return new DmarcCheckResult(DmarcVerdict.TempError, DmarcPolicyLocal.None, $"error: {ex.Message}");
        }
    }

    private async Task<string?> GetDmarcRecordAsync(string domain, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dns.QueryAsync($"_dmarc.{domain}", QueryType.TXT, cancellationToken: cancellationToken);
            return result.Answers
                .OfType<TxtRecord>()
                .Select(txt => string.Join("", txt.Text))
                .FirstOrDefault(t => t.StartsWith("v=DMARC1", StringComparison.OrdinalIgnoreCase));
        }
        catch (DnsResponseException)
        {
            return null;
        }
    }

    private static bool AreDomainAligned(string headerFromDomain, string envelopeFromDomain, bool strict)
    {
        if (strict)
            return string.Equals(headerFromDomain, envelopeFromDomain, StringComparison.OrdinalIgnoreCase);

        var headerOrg = GetOrganizationalDomain(headerFromDomain);
        var envelopeOrg = GetOrganizationalDomain(envelopeFromDomain);
        return string.Equals(headerOrg, envelopeOrg, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetOrganizationalDomain(string domain)
    {
        var parts = domain.Split('.');
        return parts.Length >= 2
            ? string.Join('.', parts[^2..])
            : domain;
    }
}
