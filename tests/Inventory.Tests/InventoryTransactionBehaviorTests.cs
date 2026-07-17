using BuildingBlocks.Application;
using Inventory.Application;
using Inventory.Application.Common.Behaviors;
using Inventory.Application.Common.Interfaces;

namespace Inventory.Tests;

public sealed class InventoryTransactionBehaviorTests
{
    [Fact]
    public async Task Already_processed_duplicate_short_circuits_before_opening_a_transaction()
    {
        var transactions = new FakeTransactionManager();
        var inbox = new FakeInbox(hasBeenProcessed: true);
        var metrics = new FakeMetrics();
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new InventoryTransactionBehavior<FakeIdempotentCommand, string>(transactions, inbox, metrics, unitOfWork);
        var nextCalled = false;

        var result = await behavior.Handle(
            new FakeIdempotentCommand(Guid.NewGuid()),
            _ =>
            {
                nextCalled = true;
                return Task.FromResult("Reserved");
            },
            CancellationToken.None);

        Assert.Equal("Duplicate", result);
        Assert.False(nextCalled);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Null(transactions.LastTransaction);
        Assert.Equal(0, inbox.TryRecordCallCount);
        Assert.Equal(1, metrics.DuplicateCount);
        Assert.Equal(0, metrics.ProcessedCount);
    }

    [Fact]
    public async Task Concurrent_duplicate_that_passes_precheck_is_caught_by_the_transactional_insert()
    {
        var transactions = new FakeTransactionManager();
        var inbox = new FakeInbox(hasBeenProcessed: false, tryRecordSucceeds: false);
        var metrics = new FakeMetrics();
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new InventoryTransactionBehavior<FakeIdempotentCommand, string>(transactions, inbox, metrics, unitOfWork);
        var nextCalled = false;

        var result = await behavior.Handle(
            new FakeIdempotentCommand(Guid.NewGuid()),
            _ =>
            {
                nextCalled = true;
                return Task.FromResult("Reserved");
            },
            CancellationToken.None);

        Assert.Equal("Duplicate", result);
        Assert.False(nextCalled);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Equal(1, inbox.TryRecordCallCount);
        Assert.True(transactions.LastTransaction!.RolledBack);
        Assert.False(transactions.LastTransaction.Committed);
        Assert.Equal(1, metrics.DuplicateCount);
        Assert.Equal(0, metrics.ProcessedCount);
    }

    [Fact]
    public async Task New_idempotent_command_invokes_handler_and_commits()
    {
        var transactions = new FakeTransactionManager();
        var inbox = new FakeInbox(hasBeenProcessed: false);
        var metrics = new FakeMetrics();
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new InventoryTransactionBehavior<FakeIdempotentCommand, string>(transactions, inbox, metrics, unitOfWork);

        var result = await behavior.Handle(new FakeIdempotentCommand(Guid.NewGuid()), _ => Task.FromResult("Reserved"), CancellationToken.None);

        Assert.Equal("Reserved", result);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.True(transactions.LastTransaction!.Committed);
        Assert.False(transactions.LastTransaction.RolledBack);
        Assert.Equal(1, metrics.ProcessedCount);
        Assert.Equal(0, metrics.DuplicateCount);
    }

    [Fact]
    public async Task Non_idempotent_command_skips_inbox_but_still_transacts()
    {
        var transactions = new FakeTransactionManager();
        var inbox = new FakeInbox(hasBeenProcessed: false);
        var metrics = new FakeMetrics();
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new InventoryTransactionBehavior<FakeCommand, string>(transactions, inbox, metrics, unitOfWork);

        var result = await behavior.Handle(new FakeCommand(), _ => Task.FromResult("Adjusted"), CancellationToken.None);

        Assert.Equal("Adjusted", result);
        Assert.Equal(0, inbox.TryRecordCallCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.True(transactions.LastTransaction!.Committed);
        Assert.Equal(0, metrics.ProcessedCount);
        Assert.Equal(0, metrics.DuplicateCount);
    }

    [Fact]
    public async Task Handler_failure_rolls_back_and_rethrows_without_saving()
    {
        var transactions = new FakeTransactionManager();
        var inbox = new FakeInbox(hasBeenProcessed: false);
        var metrics = new FakeMetrics();
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new InventoryTransactionBehavior<FakeCommand, string>(transactions, inbox, metrics, unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(new FakeCommand(), _ => throw new InvalidOperationException("boom"), CancellationToken.None));

        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.True(transactions.LastTransaction!.RolledBack);
        Assert.False(transactions.LastTransaction.Committed);
    }

    private sealed record FakeIdempotentCommand(Guid EventId) : IIdempotentCommand<string>
    {
        public string DuplicateResponse => "Duplicate";
    }

    private sealed record FakeCommand : ICommand<string>;

    private sealed class FakeTransactionManager : IInventoryTransactionManager
    {
        public FakeTransaction? LastTransaction { get; private set; }

        public Task<IInventoryTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken = default)
        {
            LastTransaction = new FakeTransaction();
            return Task.FromResult<IInventoryTransaction>(LastTransaction);
        }
    }

    private sealed class FakeTransaction : IInventoryTransaction
    {
        public bool Committed { get; private set; }

        public bool RolledBack { get; private set; }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Committed = true;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RolledBack = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeInbox(bool hasBeenProcessed, bool tryRecordSucceeds = true) : IInventoryInbox
    {
        public int HasBeenProcessedCallCount { get; private set; }

        public int TryRecordCallCount { get; private set; }

        public Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            HasBeenProcessedCallCount++;
            return Task.FromResult(hasBeenProcessed);
        }

        public Task<bool> TryRecordAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            TryRecordCallCount++;
            return Task.FromResult(tryRecordSucceeds);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeMetrics : IInventoryMetrics
    {
        public int DuplicateCount { get; private set; }

        public int ProcessedCount { get; private set; }

        public void RecordInboxDuplicate() => DuplicateCount++;

        public void RecordInboxProcessed() => ProcessedCount++;

        public void RecordReservationRejected()
        {
        }

        public void RecordReservationReserved()
        {
        }
    }
}
