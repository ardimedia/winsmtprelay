using System.Net;
using DnsClient;
using DnsClient.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using WinSmtpRelay.Security;
using WinSmtpRelay.Security.Models;

namespace WinSmtpRelay.Security.Tests;

[TestClass]
public class SpfValidatorTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task CheckAsync_NoDomain_ReturnsNone()
    {
        var dns = new LookupClient();
        var validator = new SpfValidator(dns, NullLogger<SpfValidator>.Instance);

        var result = await validator.CheckAsync(IPAddress.Loopback, "");
        Assert.AreEqual(SpfVerdict.None, result.Verdict);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task CheckAsync_GoogleDomain_ReturnsResult()
    {
        var dns = new LookupClient();
        var validator = new SpfValidator(dns, NullLogger<SpfValidator>.Instance);

        // Google's SPF should return something (not None) for a random IP
        var result = await validator.CheckAsync(IPAddress.Parse("1.2.3.4"), "google.com");
        Assert.AreNotEqual(SpfVerdict.None, result.Verdict);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task CheckAsync_NonExistentDomain_ReturnsNone()
    {
        var dns = new LookupClient();
        var validator = new SpfValidator(dns, NullLogger<SpfValidator>.Instance);

        var result = await validator.CheckAsync(IPAddress.Loopback, "this-domain-does-not-exist-12345.invalid");
        Assert.AreEqual(SpfVerdict.None, result.Verdict);
    }
}
