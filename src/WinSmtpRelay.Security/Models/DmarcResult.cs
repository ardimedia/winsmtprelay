namespace WinSmtpRelay.Security.Models;

public enum DmarcVerdict
{
    None,
    Pass,
    Fail,
    TempError,
    PermError
}

public enum DmarcPolicy
{
    None,
    Quarantine,
    Reject
}

public record DmarcCheckResult(DmarcVerdict Verdict, DmarcPolicy Policy, string Explanation);
