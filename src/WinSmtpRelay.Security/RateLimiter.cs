using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace WinSmtpRelay.Security;

public class RateLimiter
{
    private readonly ConcurrentDictionary<string, UserSendRecord> _records = new();
    private readonly ILogger<RateLimiter> _logger;

    public RateLimiter(ILogger<RateLimiter> logger)
    {
        _logger = logger;
    }

    public bool IsAllowed(string username, int? limitPerMinute, int? limitPerDay)
    {
        if (limitPerMinute is null && limitPerDay is null)
            return true;

        var record = _records.GetOrAdd(username, _ => new UserSendRecord());
        var now = DateTime.UtcNow;

        lock (record)
        {
            record.PruneOlderThan(now.AddDays(-1));

            if (limitPerMinute.HasValue)
            {
                var countLastMinute = record.CountSince(now.AddMinutes(-1));
                if (countLastMinute >= limitPerMinute.Value)
                {
                    _logger.LogWarning("Rate limit exceeded for {User}: {Count}/{Limit} per minute",
                        username, countLastMinute, limitPerMinute.Value);
                    return false;
                }
            }

            if (limitPerDay.HasValue)
            {
                var countLastDay = record.CountSince(now.AddDays(-1));
                if (countLastDay >= limitPerDay.Value)
                {
                    _logger.LogWarning("Rate limit exceeded for {User}: {Count}/{Limit} per day",
                        username, countLastDay, limitPerDay.Value);
                    return false;
                }
            }

            record.RecordSend(now);
            return true;
        }
    }

    private class UserSendRecord
    {
        private readonly List<DateTime> _timestamps = [];

        public void RecordSend(DateTime timestamp) => _timestamps.Add(timestamp);

        public int CountSince(DateTime since) =>
            _timestamps.Count(t => t >= since);

        public void PruneOlderThan(DateTime cutoff) =>
            _timestamps.RemoveAll(t => t < cutoff);
    }
}
