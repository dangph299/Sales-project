using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sales.Application;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// EF Core database context for Sales, combining the domain's aggregates, the Outbox/Inbox tables,
/// and ASP.NET Core Identity. On every <see cref="SaveChangesAsync"/> it maps any domain events
/// raised by tracked aggregates into Outbox rows before committing, implementing the transactional
/// outbox pattern.
/// </summary>
public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options, IExecutionContext executionContext) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    /// <summary>Gets the products table.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Gets the customers table.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Gets the orders table.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Gets the outbox messages table.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>Gets the inbox messages table.</summary>
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    /// <summary>Gets the refresh tokens table.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Maps any domain events raised by tracked aggregates into Outbox rows, then persists all
    /// pending changes (including the new Outbox rows) in the same transaction, and finally clears
    /// the aggregates' buffered events.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The number of state entries written to the database.
    /// </returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker.Entries<AggregateRoot<Guid>>()
            .Select(x => x.Entity)
            .Where(x => x.GetDomainEvents().Count > 0)
            .ToArray();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.GetDomainEvents())
            {
                var (topic, envelope) = DomainEventMapper.Map(aggregate, domainEvent, executionContext);
                OutboxMessages.Add(OutboxMessage.From(envelope, topic));
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);
        foreach (var aggregate in aggregates) aggregate.ClearDomainEvents();
        return result;
    }

    /// <summary>
    /// Configures the EF Core model: enables the <c>pg_trgm</c> Postgres extension used by trigram
    /// indexes, and applies every <c>IEntityTypeConfiguration&lt;T&gt;</c> in this assembly.
    /// </summary>
    /// <param name="builder">
    /// The model builder to configure.
    /// </param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("pg_trgm");
        builder.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);
    }
}
