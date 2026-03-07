using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Core.Tests;

[TestClass]
public class MessageQueueTests
{
    [TestMethod]
    public void QueuedMessage_DefaultStatus_IsQueued()
    {
        var message = new QueuedMessage
        {
            MessageId = "<test@example.com>",
            Sender = "sender@example.com",
            Recipients = "recipient@example.com",
            RawMessage = []
        };

        Assert.AreEqual(MessageStatus.Queued, message.Status);
        Assert.AreEqual(0, message.RetryCount);
        Assert.IsNull(message.LastError);
    }

    [TestMethod]
    public void QueuedMessage_CreatedUtc_IsSetAutomatically()
    {
        var before = DateTime.UtcNow;

        var message = new QueuedMessage
        {
            MessageId = "<test@example.com>",
            Sender = "sender@example.com",
            Recipients = "recipient@example.com",
            RawMessage = []
        };

        Assert.IsTrue(message.CreatedUtc >= before);
        Assert.IsTrue(message.CreatedUtc <= DateTime.UtcNow);
    }
}
