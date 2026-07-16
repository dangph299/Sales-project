using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure;

/// <summary>
/// Records failed inbound Inventory integration-event attempts.
/// </summary>
public sealed class InventoryInboxFailureRecorder : EfInboxFailureRecorder<InventoryDbContext>
{
    private readonly InventoryDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryInboxFailureRecorder"/> class.
    /// </summary>
    public InventoryInboxFailureRecorder(InventoryDbContext db, IOptions<InboxConsumerOptions> options)
        : base(db, options)
    {
        _db = db;
    }

    /// <inheritdoc/>
    protected override DbSet<InboxMessage> Inbox => _db.Inbox;

    /// <inheritdoc/>
    protected override string Consumer => "inventory-v1";
}
