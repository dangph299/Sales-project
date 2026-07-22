namespace Sales.Domain;

/// <summary>
/// The single place that turns a customer phone number into its stored and searchable forms.
/// </summary>
/// <remarks>
/// Both <see cref="Customer"/> and the order customer snapshot depend on this type, so a phone
/// stored on an order and the same phone stored on a customer always normalize identically and a
/// search term normalized here always matches what was written. Nothing else in the solution may
/// re-implement digit stripping or reversal.
/// </remarks>
public static class CustomerPhoneNormalizer
{
    private const int MinimumDigitCount = 9;
    private const int MaximumDigitCount = 15;

    /// <summary>
    /// Normalizes a phone number that is about to be stored as business data.
    /// </summary>
    /// <param name="customerPhone">Raw phone number, in any format.</param>
    /// <returns>Digits-only phone number containing 9 to 15 digits.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="customerPhone"/> is empty or whitespace, or does not contain 9 to 15 digits.</exception>
    public static string Normalize(string customerPhone)
    {
        if (string.IsNullOrWhiteSpace(customerPhone))
        {
            throw new DomainException("Phone is required.");
        }

        var normalizedCustomerPhone = KeepDigits(customerPhone);
        if (normalizedCustomerPhone.Length is < MinimumDigitCount or > MaximumDigitCount)
        {
            throw new DomainException("Phone must contain 9 to 15 digits.");
        }

        return normalizedCustomerPhone;
    }

    /// <summary>
    /// Normalizes a phone number typed into a search box.
    /// </summary>
    /// <remarks>
    /// Deliberately does not apply the 9-to-15-digit rule: a search term is a fragment of a phone
    /// number, not a phone number.
    /// </remarks>
    /// <param name="customerPhoneSearchTerm">Raw search term, in any format, possibly <see langword="null"/>.</param>
    /// <returns>Digits-only search term, or an empty string when the term holds no digit.</returns>
    public static string NormalizeSearchTerm(string? customerPhoneSearchTerm)
    {
        return KeepDigits(customerPhoneSearchTerm);
    }

    /// <summary>
    /// Reverses an already normalized phone value, so that a suffix search can run as an indexed
    /// prefix scan against the reversed column.
    /// </summary>
    /// <param name="normalizedCustomerPhone">A value already produced by <see cref="Normalize"/> or
    /// <see cref="NormalizeSearchTerm"/>. An empty string is accepted and returns an empty string,
    /// because a search term may legitimately be empty.</param>
    /// <returns><paramref name="normalizedCustomerPhone"/> with its digits in reverse order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="normalizedCustomerPhone"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="normalizedCustomerPhone"/> contains a character that is not a digit, which means the caller skipped normalization.</exception>
    public static string Reverse(string normalizedCustomerPhone)
    {
        ArgumentNullException.ThrowIfNull(normalizedCustomerPhone);

        if (!normalizedCustomerPhone.All(char.IsDigit))
        {
            throw new ArgumentException(
                "Phone must be normalized to digits before it is reversed.",
                nameof(normalizedCustomerPhone));
        }

        var reversedCustomerPhone = normalizedCustomerPhone.ToCharArray();
        Array.Reverse(reversedCustomerPhone);
        return new string(reversedCustomerPhone);
    }

    /// <summary>
    /// Reports whether a raw phone number holds the digit count required of stored business data.
    /// </summary>
    /// <remarks>
    /// Lets a request validator surface a readable field error before the aggregate throws.
    /// <see cref="Normalize"/> remains the correctness boundary; this is only a pre-check.
    /// </remarks>
    /// <param name="customerPhone">Raw phone number, in any format, possibly <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the value contains 9 to 15 digits; otherwise <see langword="false"/>.</returns>
    public static bool HasPersistableDigitCount(string? customerPhone)
    {
        var digitCount = KeepDigits(customerPhone).Length;
        return digitCount is >= MinimumDigitCount and <= MaximumDigitCount;
    }

    private static string KeepDigits(string? customerPhone)
    {
        if (string.IsNullOrWhiteSpace(customerPhone))
        {
            return string.Empty;
        }

        return new string(customerPhone.Where(char.IsDigit).ToArray());
    }
}
