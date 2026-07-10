using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure;

/// <summary>
/// EF Core database context for Inventory, combining stock items, reservations, and the Outbox/Inbox
/// tables. Unlike Sales, outbox rows are enqueued explicitly via <see cref="Enqueue"/> rather than
/// derived from domain events, since Inventory does not raise domain events.
/// </summary>
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    private readonly List<OutboxMessage> _pending = [];

    /// <summary>Gets the inventory items table.</summary>
    public DbSet<InventoryItem> Items => Set<InventoryItem>();

    /// <summary>Gets the reservations table.</summary>
    public DbSet<Reservation> Reservations => Set<Reservation>();

    /// <summary>Gets the inbox rows table.</summary>
    public DbSet<InboxRow> Inbox => Set<InboxRow>();

    /// <summary>Gets the outbox rows table.</summary>
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    /// <summary>
    /// Buffers an event to be persisted as an outbox row on the next <see cref="SaveChangesAsync"/>.
    /// </summary>
    /// <param name="envelope">
    /// The event envelope to enqueue.
    /// </param>
    /// <param name="topic">
    /// The Kafka topic the event must be published to.
    /// </param>
    public void Enqueue(EventEnvelope envelope, string topic) => _pending.Add(OutboxMessage.From(envelope, topic));

    /// <summary>
    /// Flushes any events buffered via <see cref="Enqueue"/> into the Outbox table, then persists
    /// all pending changes in the same transaction.
    /// </summary>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The number of state entries written to the database.
    /// </returns>
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        if (_pending.Count > 0) { Outbox.AddRange(_pending); _pending.Clear(); }
        return base.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Applies every <c>IEntityTypeConfiguration&lt;T&gt;</c> in this assembly.
    /// </summary>
    /// <param name="builder">
    /// The model builder to configure.
    /// </param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}
