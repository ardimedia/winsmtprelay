namespace WinSmtpRelay.Core.Configuration;

public class StatisticsOptions
{
    public const string SectionName = "Statistics";
    public int RetentionDays { get; set; } = 90;
    public string AggregationTimeUtc { get; set; } = "00:00";
}
