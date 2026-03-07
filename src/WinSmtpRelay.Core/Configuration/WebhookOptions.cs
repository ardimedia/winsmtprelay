namespace WinSmtpRelay.Core.Configuration;

public class WebhookOptions
{
    public const string SectionName = "Webhooks";

    public List<WebhookEndpoint> OnMessageReceived { get; set; } = [];
}

public class WebhookEndpoint
{
    public string Url { get; set; } = "";
    public string? Secret { get; set; }
    public int TimeoutSeconds { get; set; } = 10;
}
