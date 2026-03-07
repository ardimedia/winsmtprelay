namespace WinSmtpRelay.Core.Models;

public class DeliveryLog
{
    public long Id { get; set; }
    public long QueuedMessageId { get; set; }
    public required string Recipient { get; set; }
    public required string StatusCode { get; set; }
    public required string StatusMessage { get; set; }
    public string? RemoteServer { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
