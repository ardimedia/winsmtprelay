namespace WinSmtpRelay.Core.Interfaces;

public interface IActivityNotifier
{
    Task NotifyMessageReceivedAsync(string messageId, string sender, string recipients, int sizeBytes);
    Task NotifyDeliveryAttemptAsync(string messageId, string recipient, string statusCode, string? remoteServer);
    Task NotifyConnectionAsync(string sourceIp, string eventType);
}
