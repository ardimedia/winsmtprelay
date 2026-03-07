using Microsoft.Extensions.Logging.Abstractions;
using WinSmtpRelay.Security;

namespace WinSmtpRelay.Security.Tests;

[TestClass]
public class RateLimiterTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void IsAllowed_NoLimits_AlwaysAllowed()
    {
        var limiter = new RateLimiter(NullLogger<RateLimiter>.Instance);
        Assert.IsTrue(limiter.IsAllowed("user1", null, null));
        Assert.IsTrue(limiter.IsAllowed("user1", null, null));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void IsAllowed_PerMinuteLimit_BlocksAfterExceeded()
    {
        var limiter = new RateLimiter(NullLogger<RateLimiter>.Instance);

        // Allow 3 per minute
        Assert.IsTrue(limiter.IsAllowed("user1", 3, null));
        Assert.IsTrue(limiter.IsAllowed("user1", 3, null));
        Assert.IsTrue(limiter.IsAllowed("user1", 3, null));
        Assert.IsFalse(limiter.IsAllowed("user1", 3, null)); // 4th should be blocked
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void IsAllowed_DifferentUsers_IndependentLimits()
    {
        var limiter = new RateLimiter(NullLogger<RateLimiter>.Instance);

        Assert.IsTrue(limiter.IsAllowed("user1", 1, null));
        Assert.IsFalse(limiter.IsAllowed("user1", 1, null));

        // user2 should still be allowed
        Assert.IsTrue(limiter.IsAllowed("user2", 1, null));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void IsAllowed_PerDayLimit_BlocksAfterExceeded()
    {
        var limiter = new RateLimiter(NullLogger<RateLimiter>.Instance);

        Assert.IsTrue(limiter.IsAllowed("user1", null, 2));
        Assert.IsTrue(limiter.IsAllowed("user1", null, 2));
        Assert.IsFalse(limiter.IsAllowed("user1", null, 2));
    }
}
