namespace WinSmtpRelay.Core.Models;

public class DailyStatistics
{
    public DateOnly Date { get; set; }
    public int TotalSent { get; set; }
    public int TotalFailed { get; set; }
    public int TotalBounced { get; set; }
    public double AverageDeliveryTimeMs { get; set; }
    public DateTime ComputedAtUtc { get; set; }
}
