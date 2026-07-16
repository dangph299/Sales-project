using BuildingBlocks.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Creates audit events from pending EF Core changes.
/// </summary>
public interface IAuditEntryFactory
{
    /// <summary>
    /// Creates audit events for the pending changes in the DbContext.
    /// </summary>
    /// <param name="dbContext">DbContext being saved.</param>
    /// <param name="serviceName">Service producing audit events.</param>
    /// <returns>Audit events ready to enqueue in the service outbox.</returns>
    IReadOnlyCollection<AuditLogEvent> CreateAuditEvents(DbContext dbContext, string serviceName);
}
