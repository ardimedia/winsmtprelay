using Microsoft.EntityFrameworkCore;
using WinSmtpRelay.Core.Interfaces;
using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Storage;

public class StatisticsService(RelayDbContext db) : IStatisticsService
{
    public async Task<IReadOnlyList<TimeBucketResult>> GetLiveStatisticsAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-60);
        var now = DateTime.UtcNow;

        var logs = await db.DeliveryLogs
            .AsNoTracking()
            .Where(l => l.TimestampUtc >= cutoff)
            .Select(l => new { l.TimestampUtc, l.StatusCode })
            .ToListAsync(ct);

        var buckets = new TimeBucketResult[60];
        for (var i = 0; i < 60; i++)
        {
            var second = now.AddSeconds(-(59 - i));
            var key = second.Second;
            var matching = logs.Where(l => (int)(now - l.TimestampUtc).TotalSeconds == 59 - i);
            buckets[i] = new TimeBucketResult(
                second.ToString("HH:mm:ss"),
                matching.Count(l => l.StatusCode == "250"),
                matching.Count(l => l.StatusCode.StartsWith('5')));
        }

        return buckets;
    }

    public async Task<IReadOnlyList<TimeBucketResult>> GetHourlyStatisticsAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var now = DateTime.UtcNow;

        var logs = await db.DeliveryLogs
            .AsNoTracking()
            .Where(l => l.TimestampUtc >= cutoff)
            .Select(l => new { l.TimestampUtc, l.StatusCode })
            .ToListAsync(ct);

        var buckets = new TimeBucketResult[60];
        for (var i = 0; i < 60; i++)
        {
            var minute = now.AddMinutes(-(59 - i));
            var matching = logs.Where(l => (int)(now - l.TimestampUtc).TotalMinutes >= 59 - i
                                        && (int)(now - l.TimestampUtc).TotalMinutes < 60 - i);
            buckets[i] = new TimeBucketResult(
                minute.ToString("HH:mm"),
                matching.Count(l => l.StatusCode == "250"),
                matching.Count(l => l.StatusCode.StartsWith('5')));
        }

        return buckets;
    }

    public async Task<IReadOnlyList<TimeBucketResult>> GetDailyStatisticsAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);
        var now = DateTime.UtcNow;

        var grouped = await db.DeliveryLogs
            .AsNoTracking()
            .Where(l => l.TimestampUtc >= cutoff)
            .GroupBy(l => l.TimestampUtc.Hour)
            .Select(g => new
            {
                Hour = g.Key,
                Sent = g.Count(l => l.StatusCode == "250"),
                Failed = g.Count(l => l.StatusCode.StartsWith("5"))
            })
            .ToListAsync(ct);

        var buckets = new TimeBucketResult[24];
        for (var i = 0; i < 24; i++)
        {
            var hour = now.AddHours(-(23 - i));
            var hourKey = hour.Hour;
            var match = grouped.FirstOrDefault(g => g.Hour == hourKey);
            buckets[i] = new TimeBucketResult(
                hour.ToString("HH:00"),
                match?.Sent ?? 0,
                match?.Failed ?? 0);
        }

        return buckets;
    }

    public async Task<IReadOnlyList<DailyBucketResult>> GetMonthlyStatisticsAsync(CancellationToken ct = default)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30);

        var rows = await db.DailyStatistics
            .AsNoTracking()
            .Where(d => d.Date >= cutoff)
            .OrderBy(d => d.Date)
            .ToListAsync(ct);

        // Zero-fill missing days
        var results = new List<DailyBucketResult>();
        for (var i = 0; i < 30; i++)
        {
            var date = cutoff.AddDays(i + 1);
            var match = rows.FirstOrDefault(r => r.Date == date);
            results.Add(new DailyBucketResult(
                date.ToString("yyyy-MM-dd"),
                match?.TotalSent ?? 0,
                match?.TotalFailed ?? 0,
                match?.TotalBounced ?? 0));
        }

        return results;
    }

    public async Task AggregateDayAsync(DateOnly date, CancellationToken ct = default)
    {
        var startUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = startUtc.AddDays(1);

        var logs = await db.DeliveryLogs
            .AsNoTracking()
            .Where(l => l.TimestampUtc >= startUtc && l.TimestampUtc < endUtc)
            .Select(l => new { l.StatusCode, l.QueuedMessageId, l.TimestampUtc })
            .ToListAsync(ct);

        var sent = logs.Count(l => l.StatusCode == "250");
        var failed = logs.Count(l => l.StatusCode.StartsWith('5'));
        var bounced = logs.Count(l => l.StatusCode.StartsWith('4'));

        // Calculate average delivery time by joining with QueuedMessages
        var avgDeliveryMs = 0.0;
        var messageIds = logs.Select(l => l.QueuedMessageId).Distinct().ToList();
        if (messageIds.Count > 0)
        {
            var messages = await db.QueuedMessages
                .AsNoTracking()
                .Where(m => messageIds.Contains(m.Id))
                .Select(m => new { m.Id, m.CreatedUtc })
                .ToListAsync(ct);

            var deliveryTimes = logs
                .Join(messages, l => l.QueuedMessageId, m => m.Id,
                    (l, m) => (l.TimestampUtc - m.CreatedUtc).TotalMilliseconds)
                .Where(ms => ms > 0)
                .ToList();

            if (deliveryTimes.Count > 0)
                avgDeliveryMs = deliveryTimes.Average();
        }

        var existing = await db.DailyStatistics.FindAsync([date], ct);
        if (existing is not null)
        {
            existing.TotalSent = sent;
            existing.TotalFailed = failed;
            existing.TotalBounced = bounced;
            existing.AverageDeliveryTimeMs = avgDeliveryMs;
            existing.ComputedAtUtc = DateTime.UtcNow;
        }
        else
        {
            db.DailyStatistics.Add(new DailyStatistics
            {
                Date = date,
                TotalSent = sent,
                TotalFailed = failed,
                TotalBounced = bounced,
                AverageDeliveryTimeMs = avgDeliveryMs,
                ComputedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task BackfillAsync(CancellationToken ct = default)
    {
        var earliest = await db.DeliveryLogs
            .AsNoTracking()
            .OrderBy(l => l.TimestampUtc)
            .Select(l => (DateTime?)l.TimestampUtc)
            .FirstOrDefaultAsync(ct);

        if (earliest is null)
            return;

        var startDate = DateOnly.FromDateTime(earliest.Value);
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        for (var date = startDate; date <= yesterday; date = date.AddDays(1))
        {
            ct.ThrowIfCancellationRequested();
            await AggregateDayAsync(date, ct);
        }
    }

    public async Task PurgeOldStatisticsAsync(int retentionDays, CancellationToken ct = default)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-retentionDays);
        await db.DailyStatistics
            .Where(d => d.Date < cutoff)
            .ExecuteDeleteAsync(ct);
    }
}
