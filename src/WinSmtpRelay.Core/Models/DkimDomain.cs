namespace WinSmtpRelay.Core.Models;

public class DkimDomain
{
    public int Id { get; set; }
    public required string Domain { get; set; }
    public required string Selector { get; set; }
    public required string PrivateKeyPath { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
