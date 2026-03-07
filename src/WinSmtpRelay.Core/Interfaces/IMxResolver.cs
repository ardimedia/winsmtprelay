namespace WinSmtpRelay.Core.Interfaces;

public interface IMxResolver
{
    Task<IReadOnlyList<string>> ResolveMxAsync(string domain, CancellationToken cancellationToken = default);
}
