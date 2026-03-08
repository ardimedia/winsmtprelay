namespace WinSmtpRelay.Core.Interfaces;

/// <summary>
/// Singleton service that records queue depth every second,
/// providing a server-side ring buffer for dashboard charts.
/// </summary>
public interface IQueueDepthRecorder
{
    /// <summary>Current queue depth (latest recorded value).</summary>
    int CurrentDepth { get; }

    /// <summary>
    /// Returns the last 60 seconds of queue depth keyed by "HH:mm:ss" (UTC).
    /// </summary>
    IReadOnlyDictionary<string, int> GetHistory();
}
