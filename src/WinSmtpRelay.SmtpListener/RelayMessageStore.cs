using System.Buffers;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using WinSmtpRelay.Core.Interfaces;
using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.SmtpListener;

public class RelayMessageStore : MessageStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RelayMessageStore> _logger;

    public RelayMessageStore(IServiceScopeFactory scopeFactory, ILogger<RelayMessageStore> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public override async Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        var rawMessage = buffer.ToArray();
        var sender = transaction.From.AsAddress();
        var recipients = string.Join(";", transaction.To.Select(m => m.AsAddress()));
        var messageId = ExtractMessageId(rawMessage) ?? $"<{Guid.NewGuid()}@winsmtprelay>";

        var remoteEndPoint = context.Properties.TryGetValue("RemoteEndPoint", out var ep)
            ? ep as IPEndPoint
            : null;

        var sourceIp = remoteEndPoint?.Address.ToString();

        using var scope = _scopeFactory.CreateScope();
        var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();

        var message = new QueuedMessage
        {
            MessageId = messageId,
            Sender = sender,
            Recipients = recipients,
            RawMessage = rawMessage,
            SizeBytes = rawMessage.Length,
            SourceIp = sourceIp,
            NextRetryUtc = DateTime.UtcNow
        };

        var id = await queue.EnqueueAsync(message, cancellationToken);

        _logger.LogInformation(
            "Message {MessageId} queued (id={QueueId}) from {Sender} to {Recipients} ({Size} bytes) via {SourceIp}",
            messageId, id, sender, recipients, rawMessage.Length, sourceIp ?? "unknown");

        return SmtpResponse.Ok;
    }

    private static string? ExtractMessageId(byte[] rawMessage)
    {
        // Quick scan for Message-ID header in the raw bytes
        var text = System.Text.Encoding.ASCII.GetString(rawMessage, 0, Math.Min(rawMessage.Length, 4096));
        var idx = text.IndexOf("Message-ID:", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            idx = text.IndexOf("Message-Id:", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;

        var start = text.IndexOf('<', idx);
        var end = text.IndexOf('>', start + 1);
        if (start >= 0 && end > start)
            return text[start..(end + 1)];

        return null;
    }
}
