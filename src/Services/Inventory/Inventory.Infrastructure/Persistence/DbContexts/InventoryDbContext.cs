using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure;

/// <summary>
/// Persistence state for Inventory stock, reservations, and reliable messaging.
/// </summary>
public sealed class InventoryDbContext(
    DbContextOptions<InventoryDbContext> options,
    IOutboxSignal? outboxSignal = null) : DbContext(options)
{
    private readonly List<OutboxMessage> _pending = [];

    /// <summary>Inventory items.</summary>
    public DbSet<InventoryItem> Items => Set<InventoryItem>();

    /// <summary>Stock reservations.</summary>
    public DbSet<Reservation> Reservations => Set<Reservation>();

    /// <summary>Processed inbound messages.</summary>
    public DbSet<InboxMessage> Inbox => Set<InboxMessage>();

    /// <summary>Outbound messages awaiting publication.</summary>
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    /// <summary>
    /// Buffers an event for reliable publication with the next save.
    /// </summary>
    /// <param name="envelope">Event envelope to enqueue.</param>
    /// <param name="topic">Destination topic.</param>
    public void Enqueue(EventEnvelope envelope, string topic) => _pending.Add(OutboxMessage.From(envelope, topic));

    /// <summary>
    /// Persists pending state changes and buffered integration events together.
    /// </summary>
    /// <returns>Number of state entries written to the database.</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var hasOutboxMessages = _pending.Count > 0;
        if (hasOutboxMessages) { Outbox.AddRange(_pending); _pending.Clear(); }
        var result = await base.SaveChangesAsync(ct);
        if (hasOutboxMessages) outboxSignal?.Notify();
        return result;
    }

    /// <summary>
    /// Applies every <c>IEntityTypeConfiguration&lt;T&gt;</c> in this assembly.
    /// </summary>
    /// <param name="builder">Model builder to configure.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}
