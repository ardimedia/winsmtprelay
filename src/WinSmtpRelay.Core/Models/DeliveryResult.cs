namespace WinSmtpRelay.Core.Models;

public class DeliveryResult
{
    public required string Recipient { get; init; }
    public required string StatusCode { get; init; }
    public required string StatusMessage { get; init; }
    public string? RemoteServer { get; init; }
    public bool Success => StatusCode.StartsWith("2");
}
