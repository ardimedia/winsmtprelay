namespace WinSmtpRelay.Core.Configuration;

public class SmtpListenerOptions
{
    public const string SectionName = "SmtpListener";

    public List<EndpointOptions> Endpoints { get; set; } = [
        new() { Port = 25, RequireTls = false, RequireAuth = false }
    ];

    public int MaxMessageSizeBytes { get; set; } = 25 * 1024 * 1024; // 25 MB
    public int MaxConnections { get; set; } = 100;
    public List<string> AllowedNetworks { get; set; } = ["10.0.0.0/8", "192.168.0.0/16", "172.16.0.0/12", "127.0.0.1/32"];
    public List<string> AcceptedDomains { get; set; } = [];
    public string? PickupFolder { get; set; }
    public int PickupFolderPollIntervalSeconds { get; set; } = 5;
}

public class EndpointOptions
{
    public string Address { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 25;
    public bool RequireTls { get; set; }
    public bool ImplicitTls { get; set; }
    public bool RequireAuth { get; set; }
}
