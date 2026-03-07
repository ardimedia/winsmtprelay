using WinSmtpRelay.Core.Configuration;
using WinSmtpRelay.Delivery;

namespace WinSmtpRelay.Delivery.Tests;

[TestClass]
public class RetryLogicTests
{
    private static DeliveryOptions DefaultOptions => new()
    {
        RetryIntervalsMinutes = [1, 5, 30, 120, 480, 1440],
        MaxRetryHours = 48
    };

    [TestMethod]
    public void CalculateNextRetry_FirstRetry_ReturnsInOneMinute()
    {
        var before = DateTime.UtcNow;
        var result = DeliveryWorker.CalculateNextRetry(1, DefaultOptions);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value >= before.AddMinutes(1));
        Assert.IsTrue(result.Value <= DateTime.UtcNow.AddMinutes(1).AddSeconds(5));
    }

    [TestMethod]
    public void CalculateNextRetry_SecondRetry_ReturnsInFiveMinutes()
    {
        var before = DateTime.UtcNow;
        var result = DeliveryWorker.CalculateNextRetry(2, DefaultOptions);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value >= before.AddMinutes(5));
    }

    [TestMethod]
    public void CalculateNextRetry_ZeroRetry_ReturnsNow()
    {
        var before = DateTime.UtcNow;
        var result = DeliveryWorker.CalculateNextRetry(0, DefaultOptions);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value >= before);
        Assert.IsTrue(result.Value <= DateTime.UtcNow.AddSeconds(1));
    }

    [TestMethod]
    public void CalculateNextRetry_ExceedsMaxRetryHours_ReturnsNull()
    {
        // Total of all intervals: 1+5+30+120+480+1440 = 2076 minutes = 34.6 hours
        // After 6 retries, repeating last interval (1440=24h), total > 48h
        var config = new DeliveryOptions
        {
            RetryIntervalsMinutes = [1, 5, 30, 120, 480, 1440],
            MaxRetryHours = 48
        };

        // 7th retry: total = 2076 + 1440 = 3516 min = 58.6h > 48h
        var result = DeliveryWorker.CalculateNextRetry(7, config);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void CalculateNextRetry_EmptyIntervals_ReturnsNull()
    {
        var config = new DeliveryOptions { RetryIntervalsMinutes = [] };
        var result = DeliveryWorker.CalculateNextRetry(1, config);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void CalculateNextRetry_BeyondIntervalArray_UsesLastInterval()
    {
        var config = new DeliveryOptions
        {
            RetryIntervalsMinutes = [1, 5],
            MaxRetryHours = 48
        };

        var before = DateTime.UtcNow;
        // 3rd retry: index = min(2, 1) = 1 -> 5 minutes
        var result = DeliveryWorker.CalculateNextRetry(3, config);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value >= before.AddMinutes(5));
    }
}
