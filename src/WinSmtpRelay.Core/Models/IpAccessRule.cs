namespace WinSmtpRelay.Core.Models;

public class IpAccessRule
{
    public int Id { get; set; }
    public required string Network { get; set; }
    public IpAccessAction Action { get; set; } = IpAccessAction.Allow;
    public int SortOrder { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public enum IpAccessAction
{
    Allow,
    Deny
}
