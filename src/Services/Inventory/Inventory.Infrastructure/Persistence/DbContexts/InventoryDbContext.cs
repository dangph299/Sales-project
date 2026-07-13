using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure;

/// <summary>
/// Persistence state for Inventory stock, reservations, and reliable messaging.
/// </summary>
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    private readonly List<OutboxMessage> _pending = [];

    /// <summary>Inventory items.</summary>
    public DbSet<InventoryItem> Items => Set<InventoryItem>();

    /// <summary>Stock reservations.</summary>
    public DbSet<Reservation> Reservations => Set<Reservation>();

    /// <summary>Processed inbound messages.</summary>
    public DbSet<InboxRow> Inbox => Set<InboxRow>();

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
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of state entries written to the database.</returns>
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        if (_pending.Count > 0) { Outbox.AddRange(_pending); _pending.Clear(); }
        return base.SaveChangesAsync(ct);
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
