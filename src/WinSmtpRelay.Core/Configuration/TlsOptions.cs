namespace WinSmtpRelay.Core.Configuration;

public class TlsOptions
{
    public const string SectionName = "Tls";

    /// <summary>PFX file path. If set, takes priority over CertificateThumbprint.</summary>
    public string? CertificatePath { get; set; }

    /// <summary>Password for the PFX file.</summary>
    public string? CertificatePassword { get; set; }

    /// <summary>Thumbprint to load from Windows Certificate Store (LocalMachine\My).</summary>
    public string? CertificateThumbprint { get; set; }
}
