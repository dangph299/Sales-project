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
        // Codes are zero-padded to a fixed width, so ordering by length then by the code itself puts
        // the highest sequence last and lets the database return a single row. Reading every code and
        // taking the maximum client-side would scan the whole customer table on every insert.
        var highestCode = await db.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(customer => customer.CustomerCode.StartsWith(Prefix))
            .OrderByDescending(customer => customer.CustomerCode.Length)
            .ThenByDescending(customer => customer.CustomerCode)
            .Select(customer => customer.CustomerCode)
            .FirstOrDefaultAsync(cancellationToken);

        var maxSequence = highestCode is null ? 0 : ParseSequence(highestCode);

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
