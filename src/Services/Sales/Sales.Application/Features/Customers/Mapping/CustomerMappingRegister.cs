using Mapster;
using Sales.Application.Features.Customers.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Customers.Mapping;

/// <summary>
/// Mapping configuration owned by the <see cref="Customer"/> aggregate root.
/// </summary>
public sealed class CustomerMappingRegister : IRegister
{
    /// <inheritdoc/>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Customer, CustomerDto>()
            .Map(
                destination => destination.Phone,
                source => source.NormalizedPhone)
            .Map(
                destination => destination.Status,
                source => source.Status.ToString());
    }
}
