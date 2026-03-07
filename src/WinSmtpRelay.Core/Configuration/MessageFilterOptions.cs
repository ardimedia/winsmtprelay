namespace WinSmtpRelay.Core.Configuration;

public class MessageFilterOptions
{
    public const string SectionName = "MessageFilters";

    public List<HeaderRewriteRule> HeaderRewrites { get; set; } = [];
    public List<SenderRewriteRule> SenderRewrites { get; set; } = [];
}

public class HeaderRewriteRule
{
    public string HeaderName { get; set; } = "";
    public string? MatchValue { get; set; }
    public string Action { get; set; } = "Set"; // Set, Remove, Append
    public string? NewValue { get; set; }
}

public class SenderRewriteRule
{
    public string FromPattern { get; set; } = "";
    public string ToAddress { get; set; } = "";
}
