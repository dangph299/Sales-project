using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sales.Api.Tests;

/// <summary>
/// Sales.Api dispatches Hangfire jobs through MediatR, and no pipeline behavior logs failures above
/// Debug - the job boundary owns that log. Nothing in this solution writes it: Hangfire does, through
/// its own log provider. That makes Hangfire's logging an unwritten dependency of the design, so it is
/// pinned here rather than assumed.
/// </summary>
public sealed class HangfireLoggingBridgeTests
{
    [Fact]
    public void Hangfire_routes_its_own_logs_into_microsoft_logging()
    {
        var captured = new CapturedLogs();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder
            .SetMinimumLevel(LogLevel.Trace)
            .AddProvider(new CapturingLoggerProvider(captured)));
        services.AddHangfire(_ => { });

        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IGlobalConfiguration>();

        Hangfire.Logging.LogProvider.GetLogger("probe")
            .Log(Hangfire.Logging.LogLevel.Error, () => "hangfire-failure-probe");

        Assert.Contains(captured.Snapshot(), x => x.Message.Contains("hangfire-failure-probe"));
    }

    [Fact]
    public async Task A_job_that_will_be_retried_is_logged_at_warning()
    {
        var log = await RunFailingJobAsync(client => client.Enqueue(() => RetryingJob.Run()), RetryingJob.Marker);

        Assert.Equal(LogLevel.Warning, log.Level);
        Assert.Contains("Retry attempt", log.Message);
    }

    [Fact]
    public async Task A_permanently_failing_job_is_logged_at_error()
    {
        var log = await RunFailingJobAsync(client => client.Enqueue(() => DoomedJob.Run()), DoomedJob.Marker);

        Assert.Equal(LogLevel.Error, log.Level);
    }

    private static async Task<LogEntry> RunFailingJobAsync(Action<IBackgroundJobClient> enqueue, string marker)
    {
        var captured = new CapturedLogs();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder
            .SetMinimumLevel(LogLevel.Trace)
            .AddProvider(new CapturingLoggerProvider(captured)));
        services.AddHangfire(config => config.UseInMemoryStorage());
        services.AddHangfireServer(options =>
        {
            options.SchedulePollingInterval = TimeSpan.FromMilliseconds(50);
            options.WorkerCount = 1;
        });

        using var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToList();
        foreach (var hostedService in hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        try
        {
            enqueue(provider.GetRequiredService<IBackgroundJobClient>());

            var failureLog = await WaitForAsync(
                captured,
                entry => entry.Level >= LogLevel.Warning && entry.Text.Contains(marker));

            Assert.NotNull(failureLog);
            return failureLog;
        }
        finally
        {
            foreach (var hostedService in hostedServices)
            {
                await hostedService.StopAsync(CancellationToken.None);
            }
        }
    }

    private static async Task<LogEntry?> WaitForAsync(CapturedLogs captured, Func<LogEntry, bool> predicate)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            var match = captured.Snapshot().FirstOrDefault(predicate);
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(50);
        }

        return null;
    }

    public static class RetryingJob
    {
        public const string Marker = "hangfire-retrying-boom";

        public static void Run() => throw new InvalidOperationException(Marker);
    }

    public static class DoomedJob
    {
        public const string Marker = "hangfire-doomed-boom";

        // Attempts = 0 makes the first failure the final one, which is the case that must reach Error.
        [AutomaticRetry(Attempts = 0)]
        public static void Run() => throw new InvalidOperationException(Marker);
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception)
    {
        public string Text => $"{Message} {Exception}";
    }

    private sealed class CapturedLogs
    {
        private readonly List<LogEntry> entries = [];

        public void Add(LogEntry entry)
        {
            lock (entries) entries.Add(entry);
        }

        public IReadOnlyList<LogEntry> Snapshot()
        {
            lock (entries) return entries.ToList();
        }
    }

    private sealed class CapturingLoggerProvider(CapturedLogs captured) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(captured);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(CapturedLogs captured) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                captured.Add(new LogEntry(logLevel, formatter(state, exception), exception));
            }
        }
    }
}
