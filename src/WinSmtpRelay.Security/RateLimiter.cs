using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WinSmtpRelay.Core.Configuration;

namespace WinSmtpRelay.Security;

public class RateLimiter
{
    private readonly ConcurrentDictionary<string, SlidingWindowCounter> _userRecords = new();
    private readonly ConcurrentDictionary<string, SlidingWindowCounter> _ipRecords = new();
    private readonly ConcurrentDictionary<string, SlidingWindowCounter> _senderRecords = new();
    private readonly ConcurrentDictionary<string, FailedAuthRecord> _failedAuthRecords = new();
    private readonly RateLimitOptions _options;
    private readonly ILogger<RateLimiter> _logger;

    public RateLimiter(IOptions<RateLimitOptions> options, ILogger<RateLimiter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsAllowed(string username, int? limitPerMinute, int? limitPerDay)
    {
        if (limitPerMinute is null && limitPerDay is null)
            return true;

        var record = _userRecords.GetOrAdd(username, _ => new SlidingWindowCounter());
        var now = DateTime.UtcNow;

        lock (record)
        {
            record.PruneOlderThan(now.AddDays(-1));

            if (limitPerMinute.HasValue && record.CountSince(now.AddMinutes(-1)) >= limitPerMinute.Value)
            {
                _logger.LogWarning("Rate limit exceeded for user {User}: per-minute limit {Limit}", username, limitPerMinute.Value);
                return false;
            }

            if (limitPerDay.HasValue && record.CountSince(now.AddDays(-1)) >= limitPerDay.Value)
            {
                _logger.LogWarning("Rate limit exceeded for user {User}: per-day limit {Limit}", username, limitPerDay.Value);
                return false;
            }

            record.Record(now);
            return true;
        }
    }

    public bool IsIpAllowed(string ipAddress)
    {
        if (_options.MaxConnectionsPerIpPerMinute <= 0) return true;

        var record = _ipRecords.GetOrAdd(ipAddress, _ => new SlidingWindowCounter());
        var now = DateTime.UtcNow;

        lock (record)
        {
            record.PruneOlderThan(now.AddMinutes(-5));

            if (record.CountSince(now.AddMinutes(-1)) >= _options.MaxConnectionsPerIpPerMinute)
            {
                _logger.LogWarning("IP rate limit exceeded for {Ip}: {Limit}/min", ipAddress, _options.MaxConnectionsPerIpPerMinute);
                return false;
            }

            record.Record(now);
            return true;
        }
    }

    public bool IsSenderAllowed(string senderAddress)
    {
        if (_options.MaxMessagesPerSenderPerMinute <= 0 && _options.MaxMessagesPerSenderPerDay <= 0)
            return true;

        var record = _senderRecords.GetOrAdd(senderAddress.ToLowerInvariant(), _ => new SlidingWindowCounter());
        var now = DateTime.UtcNow;

        lock (record)
        {
            record.PruneOlderThan(now.AddDays(-1));

            if (_options.MaxMessagesPerSenderPerMinute > 0 &&
                record.CountSince(now.AddMinutes(-1)) >= _options.MaxMessagesPerSenderPerMinute)
            {
                _logger.LogWarning("Sender rate limit exceeded for {Sender}: {Limit}/min", senderAddress, _options.MaxMessagesPerSenderPerMinute);
                return false;
            }

            if (_options.MaxMessagesPerSenderPerDay > 0 &&
                record.CountSince(now.AddDays(-1)) >= _options.MaxMessagesPerSenderPerDay)
            {
                _logger.LogWarning("Sender rate limit exceeded for {Sender}: {Limit}/day", senderAddress, _options.MaxMessagesPerSenderPerDay);
                return false;
            }

            record.Record(now);
            return true;
        }
    }

    public void RecordFailedAuth(string ipAddress)
    {
        var record = _failedAuthRecords.GetOrAdd(ipAddress, _ => new FailedAuthRecord());
        lock (record)
        {
            record.FailCount++;
            record.LastFailUtc = DateTime.UtcNow;

            if (record.FailCount >= _options.FailedAuthBanThreshold)
            {
                record.BannedUntilUtc = DateTime.UtcNow.AddMinutes(_options.FailedAuthBanMinutes);
                _logger.LogWarning("IP {Ip} auto-banned for {Minutes} minutes after {Count} failed auth attempts",
                    ipAddress, _options.FailedAuthBanMinutes, record.FailCount);
            }
        }
    }

    public void ClearFailedAuth(string ipAddress)
    {
        _failedAuthRecords.TryRemove(ipAddress, out _);
    }

    public bool IsIpBanned(string ipAddress)
    {
        if (!_failedAuthRecords.TryGetValue(ipAddress, out var record))
            return false;

        lock (record)
        {
            if (record.BannedUntilUtc is null)
                return false;

            if (DateTime.UtcNow >= record.BannedUntilUtc.Value)
            {
                record.FailCount = 0;
                record.BannedUntilUtc = null;
                return false;
            }

            return true;
        }
    }

    private class SlidingWindowCounter
    {
        private readonly List<DateTime> _timestamps = [];

        public void Record(DateTime timestamp) => _timestamps.Add(timestamp);

        public int CountSince(DateTime since) =>
            _timestamps.Count(t => t >= since);

        public void PruneOlderThan(DateTime cutoff) =>
            _timestamps.RemoveAll(t => t < cutoff);
    }

    private class FailedAuthRecord
    {
        public int FailCount { get; set; }
        public DateTime LastFailUtc { get; set; }
        public DateTime? BannedUntilUtc { get; set; }
    }
}
