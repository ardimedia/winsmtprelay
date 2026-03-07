using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using WinSmtpRelay.Core.Configuration;
using WinSmtpRelay.Security;

namespace WinSmtpRelay.Security.Tests;

[TestClass]
public class DkimSigningServiceTests
{
    private static string CreateTestRsaKey()
    {
        // Generate a 2048-bit RSA key in PEM format for testing
        var rsa = System.Security.Cryptography.RSA.Create(2048);
        var pem = rsa.ExportRSAPrivateKeyPem();
        var path = Path.Combine(Path.GetTempPath(), $"dkim_test_{Guid.NewGuid()}.pem");
        File.WriteAllText(path, pem);
        return path;
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Sign_WhenDisabled_DoesNothing()
    {
        var options = Options.Create(new DkimOptions { Enabled = false });
        var service = new DkimSigningService(options, NullLogger<DkimSigningService>.Instance);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Test", "test@example.com"));
        message.To.Add(new MailboxAddress("Recipient", "recipient@example.com"));
        message.Subject = "Test";
        message.Body = new TextPart("plain") { Text = "Hello" };

        service.Sign(message);

        // No DKIM-Signature header should be added
        Assert.IsNull(message.Headers[HeaderId.DkimSignature]);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Sign_WhenEnabledWithMatchingDomain_AddsDkimSignature()
    {
        var keyPath = CreateTestRsaKey();
        try
        {
            var options = Options.Create(new DkimOptions
            {
                Enabled = true,
                Domains =
                [
                    new DkimDomainConfig
                    {
                        Domain = "example.com",
                        Selector = "test",
                        PrivateKeyPath = keyPath
                    }
                ]
            });
            var service = new DkimSigningService(options, NullLogger<DkimSigningService>.Instance);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Test", "test@example.com"));
            message.To.Add(new MailboxAddress("Recipient", "recipient@other.com"));
            message.Subject = "Test Subject";
            message.Date = DateTimeOffset.UtcNow;
            message.MessageId = MimeKit.Utils.MimeUtils.GenerateMessageId();
            message.Body = new TextPart("plain") { Text = "Hello World" };

            service.Sign(message);

            var dkimHeader = message.Headers[HeaderId.DkimSignature];
            Assert.IsNotNull(dkimHeader, "DKIM-Signature header should be present");
            Assert.IsTrue(dkimHeader.Contains("d=example.com"), "DKIM signature should contain the domain");
            Assert.IsTrue(dkimHeader.Contains("s=test"), "DKIM signature should contain the selector");
        }
        finally
        {
            File.Delete(keyPath);
        }
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Sign_WhenDomainNotConfigured_DoesNothing()
    {
        var keyPath = CreateTestRsaKey();
        try
        {
            var options = Options.Create(new DkimOptions
            {
                Enabled = true,
                Domains =
                [
                    new DkimDomainConfig
                    {
                        Domain = "example.com",
                        Selector = "test",
                        PrivateKeyPath = keyPath
                    }
                ]
            });
            var service = new DkimSigningService(options, NullLogger<DkimSigningService>.Instance);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Test", "test@otherdomain.com"));
            message.To.Add(new MailboxAddress("Recipient", "recipient@example.com"));
            message.Subject = "Test";
            message.Body = new TextPart("plain") { Text = "Hello" };

            service.Sign(message);

            Assert.IsNull(message.Headers[HeaderId.DkimSignature]);
        }
        finally
        {
            File.Delete(keyPath);
        }
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void IsConfiguredForDomain_ReturnsTrueForConfiguredDomain()
    {
        var keyPath = CreateTestRsaKey();
        try
        {
            var options = Options.Create(new DkimOptions
            {
                Enabled = true,
                Domains =
                [
                    new DkimDomainConfig
                    {
                        Domain = "example.com",
                        Selector = "default",
                        PrivateKeyPath = keyPath
                    }
                ]
            });
            var service = new DkimSigningService(options, NullLogger<DkimSigningService>.Instance);

            Assert.IsTrue(service.IsConfiguredForDomain("example.com"));
            Assert.IsFalse(service.IsConfiguredForDomain("other.com"));
        }
        finally
        {
            File.Delete(keyPath);
        }
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Constructor_WhenKeyFileNotFound_LogsErrorAndContinues()
    {
        var options = Options.Create(new DkimOptions
        {
            Enabled = true,
            Domains =
            [
                new DkimDomainConfig
                {
                    Domain = "example.com",
                    Selector = "test",
                    PrivateKeyPath = @"C:\nonexistent\key.pem"
                }
            ]
        });

        // Should not throw
        var service = new DkimSigningService(options, NullLogger<DkimSigningService>.Instance);
        Assert.IsFalse(service.IsConfiguredForDomain("example.com"));
    }
}
