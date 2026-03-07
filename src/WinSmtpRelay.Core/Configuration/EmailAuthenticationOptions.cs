namespace WinSmtpRelay.Core.Configuration;

public class EmailAuthenticationOptions
{
    public const string SectionName = "EmailAuthentication";

    public bool SpfEnabled { get; set; }
    public bool DmarcEnabled { get; set; }

    /// <summary>
    /// What to do when SPF/DMARC checks fail.
    /// LogOnly = accept and log, Reject = reject with 550, Quarantine = accept but mark as quarantined.
    /// </summary>
    public EnforcementMode Enforcement { get; set; } = EnforcementMode.LogOnly;
}

public enum EnforcementMode
{
    LogOnly,
    Reject,
    Quarantine
}
