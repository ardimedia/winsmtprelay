using Microsoft.EntityFrameworkCore;
using WinSmtpRelay.Core.Interfaces;
using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Storage;

public class RateLimitSettingsService(RelayDbContext db) : IRateLimitSettingsService
{
    public async Task<RateLimitSettings> GetAsync(CancellationToken ct = default)
    {
        var settings = await db.RateLimitSettings.FindAsync([1], ct);
        if (settings is not null) return settings;

        // Defensive: create default row if missing
        settings = new RateLimitSettings { Id = 1 };
        db.RateLimitSettings.Add(settings);
        await db.SaveChangesAsync(ct);
        return settings;
    }

    public async Task UpdateAsync(RateLimitSettings settings, CancellationToken ct = default)
    {
        var existing = await db.RateLimitSettings.FindAsync([1], ct);
        if (existing is null) return;

        existing.MaxConnectionsPerIpPerMinute = settings.MaxConnectionsPerIpPerMinute;
        existing.MaxMessagesPerSenderPerMinute = settings.MaxMessagesPerSenderPerMinute;
        existing.MaxMessagesPerSenderPerDay = settings.MaxMessagesPerSenderPerDay;
        existing.FailedAuthBanThreshold = settings.FailedAuthBanThreshold;
        existing.FailedAuthBanMinutes = settings.FailedAuthBanMinutes;
        existing.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
