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
/// Persistence state for Sales aggregates, identity data, and reliable messaging.
/// </summary>
public sealed class SalesDbContext(
    DbContextOptions<SalesDbContext> options,
    IExecutionContext executionContext,
    IOutboxSignal? outboxSignal = null) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    /// <summary>Products in the sales catalog.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Sales customers.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Customer orders.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Outbound messages awaiting publication.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>Processed inbound messages.</summary>
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    /// <summary>Issued refresh tokens.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Persists pending state changes and domain events together.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of state entries written to the database.</returns>
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
        if (aggregates.Length > 0) outboxSignal?.Notify();
        return result;
    }

    /// <summary>
    /// Configures the Sales persistence model.
    /// </summary>
    /// <param name="builder">Model builder to configure.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("pg_trgm");
        builder.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);
    }
}
