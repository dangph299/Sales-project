using BuildingBlocks.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Inventory.Infrastructure;

/// <summary>
/// Kafka consumer handler for Sales' order integration events. It owns message tracing/logging and
/// delegates Inventory state changes to <see cref="IIntegrationEventProcessor"/>.
/// </summary>
/// <param name="scopes">Scope factory for per-message dependencies.</param>
/// <param name="logger">Logger used to record structured entries for each consumed message.</param>
/// <param name="activitySource">The <see cref="ActivitySource"/> used to start the tracing span for each consumed message.</param>
public sealed class InventoryEventHandler(
    IServiceScopeFactory scopes,
    ILogger<InventoryEventHandler> logger,
    ActivitySource activitySource,
    IMessageLogContext messageLogContext) : IntegrationEventHandler<InventoryEventHandler>(scopes, logger, activitySource, messageLogContext);
