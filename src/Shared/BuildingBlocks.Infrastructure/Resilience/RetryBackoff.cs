namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Shared exponential backoff schedule for reliable-messaging retries (outbox publishing and inbox
/// re-drive), so both pipelines wait the same capped, doubling delay between attempts.
/// </summary>
public static class RetryBackoff
{
    private const double MaxDelaySeconds = 300;
    private const int MaxExponent = 8;

    /// <summary>
    /// Computes the delay to wait before the next attempt, doubling per attempt and capped at five minutes.
    /// </summary>
    /// <param name="attempts">Number of attempts already made (1-based).</param>
    /// <returns>Delay before the next attempt.</returns>
    public static TimeSpan ForAttempt(int attempts)
    {
        var seconds = Math.Min(MaxDelaySeconds, Math.Pow(2, Math.Min(attempts, MaxExponent)));
        return TimeSpan.FromSeconds(seconds);
    }
}
