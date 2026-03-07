using MailKit;
using Microsoft.Extensions.Logging;

namespace WinSmtpRelay.Delivery;

internal sealed class MailKitProtocolLogger(ILogger logger) : IProtocolLogger
{
    public IAuthenticationSecretDetector? AuthenticationSecretDetector { get; set; }

    public void LogConnect(Uri uri)
    {
        logger.LogDebug("SMTP CONNECT {Uri}", uri);
    }

    public void LogClient(byte[] buffer, int offset, int count)
    {
        var line = System.Text.Encoding.UTF8.GetString(buffer, offset, count).TrimEnd();
        if (!string.IsNullOrWhiteSpace(line))
            logger.LogDebug("SMTP C: {Line}", line);
    }

    public void LogServer(byte[] buffer, int offset, int count)
    {
        var line = System.Text.Encoding.UTF8.GetString(buffer, offset, count).TrimEnd();
        if (!string.IsNullOrWhiteSpace(line))
            logger.LogDebug("SMTP S: {Line}", line);
    }

    public void Dispose() { }
}
