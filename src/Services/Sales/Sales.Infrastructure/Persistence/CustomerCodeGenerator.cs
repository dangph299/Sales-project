using Microsoft.EntityFrameworkCore;
using Sales.Application.Features.Customers.Interfaces;

namespace Sales.Infrastructure;

/// <summary>
/// Generates customer codes from the persisted customer sequence encoded in existing codes.
/// </summary>
public sealed class CustomerCodeGenerator(SalesDbContext db) : ICustomerCodeGenerator
{
    private const string Prefix = "CUS";

    /// <inheritdoc />
    public async Task<string> NextCodeAsync(CancellationToken cancellationToken = default)
    {
        var codes = await db.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(customer => customer.CustomerCode.StartsWith(Prefix))
            .Select(customer => customer.CustomerCode)
            .ToListAsync(cancellationToken);

        var maxSequence = codes
            .Select(ParseSequence)
            .DefaultIfEmpty(0)
            .Max();

        return $"{Prefix}{maxSequence + 1:D6}";
    }

    private static int ParseSequence(string customerCode)
    {
        return customerCode.Length > Prefix.Length
            && int.TryParse(customerCode[Prefix.Length..], out var sequence)
                ? sequence
                : 0;
    }
}
