using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Core.Interfaces;

public interface IStatisticsService
{
    Task<IReadOnlyList<TimeBucketResult>> GetLiveStatisticsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TimeBucketResult>> GetHourlyStatisticsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TimeBucketResult>> GetDailyStatisticsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DailyBucketResult>> GetMonthlyStatisticsAsync(CancellationToken ct = default);
    Task AggregateDayAsync(DateOnly date, CancellationToken ct = default);
    Task BackfillAsync(CancellationToken ct = default);
    Task PurgeOldStatisticsAsync(int retentionDays, CancellationToken ct = default);
}
