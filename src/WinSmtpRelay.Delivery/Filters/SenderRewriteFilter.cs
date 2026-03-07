using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using WinSmtpRelay.Core.Configuration;
using WinSmtpRelay.Core.Interfaces;

namespace WinSmtpRelay.Delivery.Filters;

public class SenderRewriteFilter : IMessageFilter
{
    private readonly MessageFilterOptions _options;
    private readonly ILogger<SenderRewriteFilter> _logger;

    public SenderRewriteFilter(IOptions<MessageFilterOptions> options, ILogger<SenderRewriteFilter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public int Order => 200;

    public async Task<MessageFilterResult> FilterAsync(MessageFilterContext context, CancellationToken cancellationToken = default)
    {
        if (_options.SenderRewrites.Count == 0)
            return MessageFilterResult.Accepted();

        var matchingRule = _options.SenderRewrites.FirstOrDefault(r =>
            !string.IsNullOrWhiteSpace(r.FromPattern) &&
            Regex.IsMatch(context.Sender, r.FromPattern, RegexOptions.IgnoreCase));

        if (matchingRule == null)
            return MessageFilterResult.Accepted();

        _logger.LogDebug("Rewriting sender from {OldSender} to {NewSender}", context.Sender, matchingRule.ToAddress);

        var mimeMessage = await MimeMessage.LoadAsync(new MemoryStream(context.RawMessage), cancellationToken);
        mimeMessage.From.Clear();
        mimeMessage.From.Add(MailboxAddress.Parse(matchingRule.ToAddress));

        context.Sender = matchingRule.ToAddress;

        using var output = new MemoryStream();
        await mimeMessage.WriteToAsync(output, cancellationToken);
        return MessageFilterResult.AcceptedWithModification(output.ToArray());
    }
}
