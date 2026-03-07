using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WinSmtpRelay.Core.Configuration;

namespace WinSmtpRelay.Security;

public class CertificateLoader
{
    private readonly TlsOptions _options;
    private readonly ILogger<CertificateLoader> _logger;

    public CertificateLoader(IOptions<TlsOptions> options, ILogger<CertificateLoader> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public X509Certificate2? LoadCertificate()
    {
        // PFX file takes priority
        if (!string.IsNullOrWhiteSpace(_options.CertificatePath))
        {
            if (!File.Exists(_options.CertificatePath))
            {
                _logger.LogError("Certificate file not found: {Path}", _options.CertificatePath);
                return null;
            }

            var cert = X509CertificateLoader.LoadPkcs12FromFile(_options.CertificatePath,
                _options.CertificatePassword,
                X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);

            _logger.LogInformation("Loaded TLS certificate from PFX: {Subject} (expires {Expiry})",
                cert.Subject, cert.NotAfter);
            return cert;
        }

        // Windows Certificate Store
        if (!string.IsNullOrWhiteSpace(_options.CertificateThumbprint))
        {
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            var certs = store.Certificates.Find(
                X509FindType.FindByThumbprint, _options.CertificateThumbprint, validOnly: false);

            if (certs.Count == 0)
            {
                _logger.LogError("Certificate with thumbprint {Thumbprint} not found in LocalMachine\\My",
                    _options.CertificateThumbprint);
                return null;
            }

            var cert = certs[0];
            _logger.LogInformation("Loaded TLS certificate from store: {Subject} (expires {Expiry})",
                cert.Subject, cert.NotAfter);
            return cert;
        }

        _logger.LogDebug("No TLS certificate configured");
        return null;
    }
}
