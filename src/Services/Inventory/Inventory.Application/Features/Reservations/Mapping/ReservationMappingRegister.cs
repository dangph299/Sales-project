using Inventory.Application.Features.Reservations.DTOs;
using Inventory.Domain;
using Mapster;

namespace Inventory.Application.Features.Reservations.Mapping;

/// <summary>
/// Mapster configuration for the Reservation aggregate's read models, including the
/// <see cref="ReservationLine"/> entity it owns.
/// </summary>
public sealed class ReservationMappingRegister : IRegister
{
    /// <inheritdoc/>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ReservationLine, ReservationLineSnapshot>();

        config.NewConfig<Reservation, ReservationSnapshot>()
            .Map(destination => destination.Status, source => source.Status.ToString())
            .Map(destination => destination.Lines, source => source.Lines);
    }
}
