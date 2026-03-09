namespace WinSmtpRelay.Core.Models;

public class AcceptedSenderDomain
{
    public int Id { get; set; }
    public required string Domain { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
