using Sales.Domain;

namespace Sales.Application.Features.Orders.Realtime;

/// <summary>
/// Minimal realtime payload that tells clients which order changed, without replacing REST reads.
/// </summary>
public sealed record OrderStatusChangedNotification(
    Guid OrderId,
    OrderStatus PreviousStatus,
    OrderStatus CurrentStatus,
    DateTimeOffset ChangedAt,
    long Version);

