using System.Diagnostics.Metrics;
using BuildingBlocks.Application;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Sales.Application.Features.Orders.Commands;
using Sales.Infrastructure;

namespace Sales.Api.Tests;

public sealed class CancelExpiredPendingOrdersJobTests
{
    [Fact]
    public async Task Job_dispatches_cancel_expired_pending_orders_command()
    {
        var currentUtc = DateTimeOffset.Parse("2026-07-15T10:00:00Z");
        var sender = new FakeSender(new CancelExpiredPendingOrdersResult(3, 2, 1, 0));
        var job = new CancelExpiredPendingOrdersJob(
            sender,
            new FakeClock(currentUtc),
            NullLogger<CancelExpiredPendingOrdersJob>.Instance);

        await job.ExecuteAsync(45, 75, CancellationToken.None);

        var command = Assert.IsType<CancelExpiredPendingOrders>(sender.Request);
        Assert.Equal(currentUtc, command.CurrentUtc);
        Assert.Equal(45, command.ExpirationMinutes);
        Assert.Equal(75, command.BatchSize);
    }

    [Fact]
    public async Task Job_records_expiration_metrics_for_the_batch()
    {
        var measurements = new List<(string Name, double Value)>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, activeListener) =>
            {
                if (instrument.Meter.Name == "Sales.Infrastructure")
                {
                    activeListener.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) => measurements.Add((instrument.Name, value)));
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) => measurements.Add((instrument.Name, value)));
        listener.Start();

        var sender = new FakeSender(new CancelExpiredPendingOrdersResult(3, 2, 1, 0));
        var job = new CancelExpiredPendingOrdersJob(
            sender,
            new FakeClock(DateTimeOffset.UtcNow),
            NullLogger<CancelExpiredPendingOrdersJob>.Instance);

        await job.ExecuteAsync(30, 100, CancellationToken.None);

        Assert.Contains(("sales.orders.expiration.scanned", 3d), measurements);
        Assert.Contains(("sales.orders.expiration.cancelled", 2d), measurements);
        Assert.Contains(("sales.orders.expiration.skipped", 1d), measurements);
        Assert.Contains(("sales.orders.expiration.failed", 0d), measurements);
        Assert.Contains(measurements, m => m.Name == "sales.orders.expiration.duration" && m.Value >= 0d);
    }

    [Fact]
    public async Task Job_rethrows_sender_exception_for_hangfire_retry()
    {
        var sender = new FakeSender(new InvalidOperationException("dispatch failed"));
        var job = new CancelExpiredPendingOrdersJob(
            sender,
            new FakeClock(DateTimeOffset.UtcNow),
            NullLogger<CancelExpiredPendingOrdersJob>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.ExecuteAsync(30, 100, CancellationToken.None));
    }

    [Fact]
    public void Sales_recurring_job_constants_are_stable()
    {
        Assert.Equal("sales-cleanup", SalesRecurringJobIds.MaintenanceCleanup);
        Assert.Equal("orders:cancel-expired", SalesRecurringJobIds.CancelExpiredPendingOrders);
    }

    [Fact]
    public void Sales_recurring_jobs_bind_a_single_root_configuration_section()
    {
        Assert.Equal("SalesRecurringJobs", SalesRecurringJobsOptions.SectionName);
    }

    private sealed class FakeClock(DateTimeOffset currentUtc) : IClock
    {
        public DateTimeOffset UtcNow { get; } = currentUtc;
    }

    private sealed class FakeSender : ISender
    {
        private readonly CancelExpiredPendingOrdersResult? result;
        private readonly Exception? exception;

        public FakeSender(CancelExpiredPendingOrdersResult result)
        {
            this.result = result;
        }

        public FakeSender(Exception exception)
        {
            this.exception = exception;
        }

        public object? Request { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Request = request;
            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult((TResponse)(object)result!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            Request = request;
            if (exception is not null)
            {
                throw exception;
            }

            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            Request = request;
            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult<object?>(result);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            return AsyncEnumerable.Empty<TResponse>();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            return AsyncEnumerable.Empty<object?>();
        }
    }
}
