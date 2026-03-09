using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using WinSmtpRelay.Core.Interfaces;
using WinSmtpRelay.Core.Models;
using WinSmtpRelay.Delivery.Filters;

namespace WinSmtpRelay.Delivery.Tests;

[TestClass]
public class MessageFilterTests
{
    private static byte[] CreateTestMessage(string from = "sender@test.com", string to = "rcpt@test.com", string subject = "Test")
    {
        var msg = new MimeMessage();
        msg.From.Add(MailboxAddress.Parse(from));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new TextPart("plain") { Text = "Hello" };

        using var ms = new MemoryStream();
        msg.WriteTo(ms);
        return ms.ToArray();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task HeaderRewriteFilter_NoRules_AcceptsUnmodified()
    {
        var cache = new StubRuntimeConfigCache();
        var filter = new HeaderRewriteFilter(cache, NullLogger<HeaderRewriteFilter>.Instance);

        var context = new MessageFilterContext
        {
            RawMessage = CreateTestMessage(),
            Sender = "sender@test.com",
            Recipients = "rcpt@test.com"
        };

        var result = await filter.FilterAsync(context);

        Assert.IsTrue(result.Accept);
        Assert.IsNull(result.ModifiedRawMessage);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task HeaderRewriteFilter_SetHeader_ModifiesMessage()
    {
        var cache = new StubRuntimeConfigCache
        {
            HeaderRewriteRules = [new HeaderRewriteEntry { HeaderName = "X-Custom", Action = "Set", NewValue = "test-value", IsEnabled = true }]
        };
        var filter = new HeaderRewriteFilter(cache, NullLogger<HeaderRewriteFilter>.Instance);

        var context = new MessageFilterContext
        {
            RawMessage = CreateTestMessage(),
            Sender = "sender@test.com",
            Recipients = "rcpt@test.com"
        };

        var result = await filter.FilterAsync(context);

        Assert.IsTrue(result.Accept);
        Assert.IsNotNull(result.ModifiedRawMessage);

        var modified = await MimeMessage.LoadAsync(new MemoryStream(result.ModifiedRawMessage));
        Assert.AreEqual("test-value", modified.Headers["X-Custom"]);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task HeaderRewriteFilter_RemoveHeader_RemovesFromMessage()
    {
        var raw = CreateTestMessage();
        // Add a header to the message first
        var msg = await MimeMessage.LoadAsync(new MemoryStream(raw));
        msg.Headers.Add("X-ToRemove", "value");
        using var ms = new MemoryStream();
        msg.WriteTo(ms);
        raw = ms.ToArray();

        var cache = new StubRuntimeConfigCache
        {
            HeaderRewriteRules = [new HeaderRewriteEntry { HeaderName = "X-ToRemove", Action = "Remove", IsEnabled = true }]
        };
        var filter = new HeaderRewriteFilter(cache, NullLogger<HeaderRewriteFilter>.Instance);

        var context = new MessageFilterContext
        {
            RawMessage = raw,
            Sender = "sender@test.com",
            Recipients = "rcpt@test.com"
        };

        var result = await filter.FilterAsync(context);

        Assert.IsTrue(result.Accept);
        Assert.IsNotNull(result.ModifiedRawMessage);

        var modified = await MimeMessage.LoadAsync(new MemoryStream(result.ModifiedRawMessage));
        Assert.IsFalse(modified.Headers.Contains("X-ToRemove"));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SenderRewriteFilter_NoRules_AcceptsUnmodified()
    {
        var cache = new StubRuntimeConfigCache();
        var filter = new SenderRewriteFilter(cache, NullLogger<SenderRewriteFilter>.Instance);

        var context = new MessageFilterContext
        {
            RawMessage = CreateTestMessage(),
            Sender = "sender@test.com",
            Recipients = "rcpt@test.com"
        };

        var result = await filter.FilterAsync(context);

        Assert.IsTrue(result.Accept);
        Assert.IsNull(result.ModifiedRawMessage);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SenderRewriteFilter_MatchingRule_RewritesSender()
    {
        var cache = new StubRuntimeConfigCache
        {
            SenderRewriteRules = [new SenderRewriteEntry { FromPattern = @".*@internal\.com", ToAddress = "noreply@public.com", IsEnabled = true }]
        };
        var filter = new SenderRewriteFilter(cache, NullLogger<SenderRewriteFilter>.Instance);

        var context = new MessageFilterContext
        {
            RawMessage = CreateTestMessage(from: "user@internal.com"),
            Sender = "user@internal.com",
            Recipients = "rcpt@test.com"
        };

        var result = await filter.FilterAsync(context);

        Assert.IsTrue(result.Accept);
        Assert.IsNotNull(result.ModifiedRawMessage);
        Assert.AreEqual("noreply@public.com", context.Sender);

        var modified = await MimeMessage.LoadAsync(new MemoryStream(result.ModifiedRawMessage));
        Assert.AreEqual("noreply@public.com", modified.From.Mailboxes.First().Address);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SenderRewriteFilter_NonMatchingRule_NoChange()
    {
        var cache = new StubRuntimeConfigCache
        {
            SenderRewriteRules = [new SenderRewriteEntry { FromPattern = @".*@other\.com", ToAddress = "noreply@public.com", IsEnabled = true }]
        };
        var filter = new SenderRewriteFilter(cache, NullLogger<SenderRewriteFilter>.Instance);

        var context = new MessageFilterContext
        {
            RawMessage = CreateTestMessage(from: "user@test.com"),
            Sender = "user@test.com",
            Recipients = "rcpt@test.com"
        };

        var result = await filter.FilterAsync(context);

        Assert.IsTrue(result.Accept);
        Assert.IsNull(result.ModifiedRawMessage);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void FilterOrder_HeaderBeforeSender()
    {
        var cache = new StubRuntimeConfigCache();
        var headerFilter = new HeaderRewriteFilter(cache, NullLogger<HeaderRewriteFilter>.Instance);
        var senderFilter = new SenderRewriteFilter(cache, NullLogger<SenderRewriteFilter>.Instance);

        Assert.IsTrue(headerFilter.Order < senderFilter.Order);
    }
}
