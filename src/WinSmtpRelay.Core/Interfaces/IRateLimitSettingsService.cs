using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Core.Interfaces;

public interface IRateLimitSettingsService
{
    Task<RateLimitSettings> GetAsync(CancellationToken ct = default);
    Task UpdateAsync(RateLimitSettings settings, CancellationToken ct = default);
}
