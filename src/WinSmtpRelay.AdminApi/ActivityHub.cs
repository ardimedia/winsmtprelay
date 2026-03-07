using Microsoft.AspNetCore.SignalR;

namespace WinSmtpRelay.AdminApi;

public class ActivityHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}

public static class ActivityNotifier
{
    public static async Task NotifyMessageReceivedAsync(
        IHubContext<ActivityHub> hub, string messageId, string sender, string recipients, int sizeBytes)
    {
        await hub.Clients.All.SendAsync("MessageReceived", new
        {
            MessageId = messageId,
            Sender = sender,
            Recipients = recipients,
            SizeBytes = sizeBytes,
            TimestampUtc = DateTime.UtcNow
        });
    }

    public static async Task NotifyDeliveryAttemptAsync(
        IHubContext<ActivityHub> hub, string messageId, string recipient, string statusCode, string? remoteServer)
    {
        await hub.Clients.All.SendAsync("DeliveryAttempt", new
        {
            MessageId = messageId,
            Recipient = recipient,
            StatusCode = statusCode,
            RemoteServer = remoteServer,
            TimestampUtc = DateTime.UtcNow
        });
    }

    public static async Task NotifyConnectionAsync(
        IHubContext<ActivityHub> hub, string sourceIp, string eventType)
    {
        await hub.Clients.All.SendAsync("SmtpConnection", new
        {
            SourceIp = sourceIp,
            EventType = eventType,
            TimestampUtc = DateTime.UtcNow
        });
    }
}
