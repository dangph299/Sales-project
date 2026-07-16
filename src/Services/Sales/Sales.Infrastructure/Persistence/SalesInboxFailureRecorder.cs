using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Sales.Infrastructure;

/// <summary>
/// Records failed inbound Sales integration-event attempts.
/// </summary>
public sealed class SalesInboxFailureRecorder : EfInboxFailureRecorder<SalesDbContext>
{
    private readonly SalesDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="SalesInboxFailureRecorder"/> class.
    /// </summary>
    public SalesInboxFailureRecorder(SalesDbContext db, IOptions<InboxConsumerOptions> options)
        : base(db, options)
    {
        _db = db;
    }

    /// <inheritdoc/>
    protected override DbSet<InboxMessage> Inbox => _db.InboxMessages;

    /// <inheritdoc/>
    protected override string Consumer => "sales-v1";
}
