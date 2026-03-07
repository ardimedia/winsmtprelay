using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using WinSmtpRelay.Core.Configuration;

namespace WinSmtpRelay.Security;

public class DkimSigningService
{
    private readonly DkimOptions _options;
    private readonly Dictionary<string, DkimSigner> _signers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DkimDomainConfig> _domainConfigs = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<DkimSigningService> _logger;

    private static readonly HeaderId[] HeadersToSign =
    [
        HeaderId.From,
        HeaderId.To,
        HeaderId.Subject,
        HeaderId.Date,
        HeaderId.MessageId
    ];

    public DkimSigningService(IOptions<DkimOptions> options, ILogger<DkimSigningService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!_options.Enabled)
            return;

        foreach (var domain in _options.Domains)
        {
            try
            {
                if (!File.Exists(domain.PrivateKeyPath))
                {
                    _logger.LogError("DKIM private key not found for domain {Domain}: {Path}",
                        domain.Domain, domain.PrivateKeyPath);
                    continue;
                }

                var privateKey = LoadPrivateKey(domain.PrivateKeyPath);
                var signer = new DkimSigner(privateKey, domain.Domain, domain.Selector)
                {
                    HeaderCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Relaxed,
                    BodyCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Relaxed
                };

                _signers[domain.Domain] = signer;
                _domainConfigs[domain.Domain] = domain;

                _logger.LogInformation("DKIM signing configured for {Domain} (selector={Selector})",
                    domain.Domain, domain.Selector);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load DKIM key for domain {Domain}", domain.Domain);
            }
        }
    }

    public bool IsConfiguredForDomain(string domain) => _signers.ContainsKey(domain);

    public void Sign(MimeMessage message)
    {
        if (!_options.Enabled)
            return;

        var senderDomain = message.From.Mailboxes.FirstOrDefault()?.Domain;
        if (senderDomain == null)
            return;

        if (!_signers.TryGetValue(senderDomain, out var signer))
            return;

        try
        {
            signer.Sign(message, HeadersToSign);
            _logger.LogDebug("DKIM signed message from {Domain}", senderDomain);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DKIM signing failed for message from {Domain}", senderDomain);
        }
    }

    private static AsymmetricKeyParameter LoadPrivateKey(string path)
    {
        using var reader = new StreamReader(path);
        var pemReader = new PemReader(reader);
        var keyObject = pemReader.ReadObject();

        return keyObject switch
        {
            AsymmetricCipherKeyPair keyPair => keyPair.Private,
            AsymmetricKeyParameter key => key,
            _ => throw new InvalidOperationException($"Unexpected key type: {keyObject.GetType()}")
        };
    }
}
