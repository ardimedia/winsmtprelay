using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WinSmtpRelay.Core.Configuration;
using WinSmtpRelay.Security;

namespace WinSmtpRelay.Security.Tests;

[TestClass]
public class RateLimiterTests
{
    private static RateLimiter CreateLimiter(RateLimitOptions? options = null)
    {
        var opts = Options.Create(options ?? new RateLimitOptions());
        return new RateLimiter(opts, NullLogger<RateLimiter>.Instance);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void IsAllowed_NoLimits_AlwaysAllowed()
    {
        var limiter = CreateLimiter();
        Assert.IsTrue(limiter.IsAllowed("user1", null, null));
        Assert.IsTrue(limiter.IsAllowed("user1", null, null));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void IsAllowed_PerMinuteLimit_BlocksAfterExceeded()
    {
        var limiter = CreateLimiter();

        Assert.IsTrue(limiter.IsAllowed("user1", 3, null));
        Assert.IsTrue(limiter.IsAllowed("user1", 3, null));
        Assert.IsTrue(limiter.IsAllowed("user1", 3, null));
        Assert.IsFalse(limiter.IsAllowed("user1", 3, null));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void IsAllowed_DifferentUsers_IndependentLimits()
    {
        var limiter = CreateLimiter();

        Assert.IsTrue(limiter.IsAllowed("user1", 1, null));
        Assert.IsFalse(limiter.IsAllowed("user1", 1, null));

        Assert.IsTrue(limiter.IsAllowed("user2", 1, null));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void IsAllowed_PerDayLimit_BlocksAfterExceeded()
    {
        var limiter = CreateLimiter();

        Assert.IsTrue(limiter.IsAllowed("user1", null, 2));
        Assert.IsTrue(limiter.IsAllowed("user1", null, 2));
        Assert.IsFalse(limiter.IsAllowed("user1", null, 2));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void IsIpAllowed_BlocksAfterExceeded()
    {
        var limiter = CreateLimiter(new RateLimitOptions { MaxConnectionsPerIpPerMinute = 3 });

        Assert.IsTrue(limiter.IsIpAllowed("192.168.1.1"));
        Assert.IsTrue(limiter.IsIpAllowed("192.168.1.1"));
        Assert.IsTrue(limiter.IsIpAllowed("192.168.1.1"));
        Assert.IsFalse(limiter.IsIpAllowed("192.168.1.1"));

        // Different IP should still be allowed
        Assert.IsTrue(limiter.IsIpAllowed("192.168.1.2"));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void IsSenderAllowed_BlocksAfterExceeded()
    {
        var limiter = CreateLimiter(new RateLimitOptions
        {
            MaxMessagesPerSenderPerMinute = 2,
            MaxMessagesPerSenderPerDay = 1000
        });

        Assert.IsTrue(limiter.IsSenderAllowed("user@example.com"));
        Assert.IsTrue(limiter.IsSenderAllowed("user@example.com"));
        Assert.IsFalse(limiter.IsSenderAllowed("user@example.com"));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void FailedAuth_BansIpAfterThreshold()
    {
        var limiter = CreateLimiter(new RateLimitOptions
        {
            FailedAuthBanThreshold = 3,
            FailedAuthBanMinutes = 30
        });

        var ip = "10.0.0.1";

        Assert.IsFalse(limiter.IsIpBanned(ip));

        limiter.RecordFailedAuth(ip);
        limiter.RecordFailedAuth(ip);
        Assert.IsFalse(limiter.IsIpBanned(ip));

        limiter.RecordFailedAuth(ip); // 3rd = threshold
        Assert.IsTrue(limiter.IsIpBanned(ip));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ClearFailedAuth_RemovesBan()
    {
        var limiter = CreateLimiter(new RateLimitOptions
        {
            FailedAuthBanThreshold = 1,
            FailedAuthBanMinutes = 60
        });

        var ip = "10.0.0.1";
        limiter.RecordFailedAuth(ip);
        Assert.IsTrue(limiter.IsIpBanned(ip));

        limiter.ClearFailedAuth(ip);
        Assert.IsFalse(limiter.IsIpBanned(ip));
    }
}
