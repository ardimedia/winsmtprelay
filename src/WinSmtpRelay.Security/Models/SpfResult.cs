namespace WinSmtpRelay.Security.Models;

public enum SpfVerdict
{
    None,
    Pass,
    Fail,
    SoftFail,
    Neutral,
    TempError,
    PermError
}

public record SpfCheckResult(SpfVerdict Verdict, string Explanation);
