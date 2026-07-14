namespace Inventory.Application;

/// <summary>
/// Command derived from a consumed Kafka integration event, deduplicated through the Inventory
/// inbox before it reaches business logic.
/// </summary>
public interface IIdempotentCommand<TResponse> : ICommand<TResponse>
{
    /// <summary>Consumed integration event id used for inbox deduplication.</summary>
    Guid EventId { get; }

    /// <summary>Response returned when <see cref="EventId"/> was already recorded in the inbox.</summary>
    TResponse DuplicateResponse { get; }
}
