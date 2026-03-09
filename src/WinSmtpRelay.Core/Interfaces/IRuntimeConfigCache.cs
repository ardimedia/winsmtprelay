using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Core.Interfaces;

/// <summary>
/// In-memory cache for runtime-editable configuration stored in SQLite.
/// Loaded lazily on first access; invalidated when Admin API modifies data.
/// </summary>
public interface IRuntimeConfigCache
{
    Task<IReadOnlyList<string>> GetAcceptedDomainsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAcceptedSenderDomainsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DomainRoute>> GetDomainRoutesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<HeaderRewriteEntry>> GetHeaderRewriteRulesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SenderRewriteEntry>> GetSenderRewriteRulesAsync(CancellationToken ct = default);

    /// <summary>
    /// Clears all cached data. Next access triggers a fresh DB load.
    /// </summary>
    void Invalidate();
}
