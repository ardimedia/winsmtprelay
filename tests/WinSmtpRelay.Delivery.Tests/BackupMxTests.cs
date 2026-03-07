using WinSmtpRelay.Core.Configuration;
using WinSmtpRelay.Core.Models;
using WinSmtpRelay.Delivery;

namespace WinSmtpRelay.Delivery.Tests;

[TestClass]
public class BackupMxTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void CalculateNextRetry_BackupMxDomain_UsesExtendedHoldTime()
    {
        var backupConfig = new DeliveryOptions
        {
            MaxRetryHours = 168, // 7 days
            RetryIntervalsMinutes = [15]
        };

        // After many retries, should still return a value within 7 days
        var result = DeliveryWorker.CalculateNextRetry(10, backupConfig);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void CalculateNextRetry_BackupMxDomain_ExceedsMaxHold_ReturnsNull()
    {
        var backupConfig = new DeliveryOptions
        {
            MaxRetryHours = 168, // 7 days = 10080 minutes
            RetryIntervalsMinutes = [15]
        };

        // 10080 / 15 = 672 retries to exhaust hold window
        // At retry 673, total = 673 * 15 = 10095 > 10080
        var result = DeliveryWorker.CalculateNextRetry(673, backupConfig);
        Assert.IsNull(result);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void CalculateNextRetry_BackupMxDomain_ReturnsCorrectInterval()
    {
        var backupConfig = new DeliveryOptions
        {
            MaxRetryHours = 168,
            RetryIntervalsMinutes = [15]
        };

        var before = DateTime.UtcNow;
        var result = DeliveryWorker.CalculateNextRetry(1, backupConfig);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value >= before.AddMinutes(15));
        Assert.IsTrue(result.Value <= DateTime.UtcNow.AddMinutes(15).AddSeconds(5));
    }
}
