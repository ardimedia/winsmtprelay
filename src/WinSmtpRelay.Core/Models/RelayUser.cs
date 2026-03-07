namespace WinSmtpRelay.Core.Models;

public class RelayUser
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? AllowedSenderAddresses { get; set; }
    public int? RateLimitPerMinute { get; set; }
    public int? RateLimitPerDay { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
