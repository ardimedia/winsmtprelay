namespace WinSmtpRelay.Core.Configuration;

public class BackupMxOptions
{
    public const string SectionName = "BackupMx";

    public bool Enabled { get; set; }
    public List<string> Domains { get; set; } = [];
    public int RetryIntervalMinutes { get; set; } = 15;
    public int MaxHoldHours { get; set; } = 168; // 7 days
}
