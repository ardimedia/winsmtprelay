namespace WinSmtpRelay.Core.Configuration;

public class AdminUiOptions
{
    public const string SectionName = "AdminUi";

    public bool Enabled { get; set; } = true;
    public int Port { get; set; } = 8025;
    public string BindAddress { get; set; } = "0.0.0.0";
}
