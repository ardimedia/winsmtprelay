using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WinSmtpRelay.Core.Configuration;
using WinSmtpRelay.Security;

namespace WinSmtpRelay.Security.Tests;

[TestClass]
public class CertificateLoaderTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void LoadCertificate_NothingConfigured_ReturnsNull()
    {
        var options = Options.Create(new TlsOptions());
        var loader = new CertificateLoader(options, NullLogger<CertificateLoader>.Instance);

        var cert = loader.LoadCertificate();
        Assert.IsNull(cert);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void LoadCertificate_FileNotFound_ReturnsNull()
    {
        var options = Options.Create(new TlsOptions
        {
            CertificatePath = @"C:\nonexistent\cert.pfx"
        });
        var loader = new CertificateLoader(options, NullLogger<CertificateLoader>.Instance);

        var cert = loader.LoadCertificate();
        Assert.IsNull(cert);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void LoadCertificate_ValidPfx_ReturnsCertificate()
    {
        // Create a self-signed cert as PFX
        var pfxPath = Path.Combine(Path.GetTempPath(), $"test_cert_{Guid.NewGuid()}.pfx");
        const string password = "testpass";

        try
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var selfSigned = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
            File.WriteAllBytes(pfxPath, selfSigned.Export(X509ContentType.Pfx, password));

            var options = Options.Create(new TlsOptions
            {
                CertificatePath = pfxPath,
                CertificatePassword = password
            });
            var loader = new CertificateLoader(options, NullLogger<CertificateLoader>.Instance);

            var cert = loader.LoadCertificate();
            Assert.IsNotNull(cert);
            Assert.AreEqual("CN=Test", cert.Subject);
        }
        finally
        {
            File.Delete(pfxPath);
        }
    }
}
