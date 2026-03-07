namespace WinSmtpRelay.Core.Configuration;

public class DkimOptions
{
    public const string SectionName = "Dkim";

    public bool Enabled { get; set; }
    public List<DkimDomainConfig> Domains { get; set; } = [];
}

public class DkimDomainConfig
{
    /// <summary>The domain to sign for (e.g., "ardimedia.com").</summary>
    public required string Domain { get; set; }

    /// <summary>DKIM selector (e.g., "default", "mail2026").</summary>
    public required string Selector { get; set; }

    /// <summary>Path to the RSA private key file (PEM format).</summary>
    public required string PrivateKeyPath { get; set; }
}
