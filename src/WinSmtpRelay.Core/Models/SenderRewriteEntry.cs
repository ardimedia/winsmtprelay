namespace WinSmtpRelay.Core.Models;

public class SenderRewriteEntry
{
    public int Id { get; set; }
    public string FromPattern { get; set; } = "";
    public string ToAddress { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
