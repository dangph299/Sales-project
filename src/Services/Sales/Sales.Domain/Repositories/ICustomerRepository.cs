namespace Sales.Domain;

/// <summary>
/// Command-side persistence contract for <see cref="Customer"/>, adding the phone lookup and the
/// coordination barrier that order creation needs to resolve-or-create a customer safely.
/// </summary>
public interface ICustomerRepository : IRepository<Customer>
{
    /// <summary>
    /// Finds the live customer holding a normalized phone number.
    /// </summary>
    /// <param name="normalizedCustomerPhone">Phone number already normalized via <see cref="PhoneNumberNormalizer.Normalize"/>.</param>
    /// <returns>Customer, or <see langword="null"/> when no live customer holds that number.</returns>
    Task<Customer?> FindByNormalizedPhoneAsync(string normalizedCustomerPhone, CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes a transaction-scoped lock on a normalized phone number, so that two concurrent
    /// requests creating an order for the same new customer cannot both decide the customer is
    /// missing and each insert one.
    /// </summary>
    /// <remarks>
    /// The lock is released when the surrounding transaction commits or rolls back; the caller does
    /// not release it.
    /// </remarks>
    /// <param name="normalizedCustomerPhone">Phone number already normalized via <see cref="PhoneNumberNormalizer.Normalize"/>.</param>
    Task AcquireNormalizedPhoneLockAsync(string normalizedCustomerPhone, CancellationToken cancellationToken = default);
}
