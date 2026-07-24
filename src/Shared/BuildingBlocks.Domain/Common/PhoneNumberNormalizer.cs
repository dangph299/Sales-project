namespace BuildingBlocks.Domain.PhoneNumbers;

/// <summary>
/// The single place that turns a phone number into its stored and searchable forms.
/// </summary>
/// <remarks>
/// Any bounded context that owns a phone number depends on this type, so a phone stored by one
/// aggregate and the same phone stored by another always normalize identically, and a search term
/// normalized here always matches what was written. Nothing else in the solution may re-implement
/// digit stripping or reversal.
/// </remarks>
public static class PhoneNumberNormalizer
{
    private const int MinimumDigitCount = 9;
    private const int MaximumDigitCount = 15;

    /// <summary>
    /// Normalizes a phone number that is about to be stored as business data.
    /// </summary>
    /// <param name="phone">Raw phone number, in any format.</param>
    /// <returns>Digits-only phone number containing 9 to 15 digits.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="phone"/> is empty or whitespace, or does not contain 9 to 15 digits.</exception>
    public static string Normalize(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new DomainException("Phone is required.");
        }

        var normalizedPhone = KeepDigits(phone);
        if (normalizedPhone.Length is < MinimumDigitCount or > MaximumDigitCount)
        {
            throw new DomainException("Phone must contain 9 to 15 digits.");
        }

        return normalizedPhone;
    }

    /// <summary>
    /// Normalizes a phone number typed into a search box.
    /// </summary>
    /// <remarks>
    /// Deliberately does not apply the 9-to-15-digit rule: a search term is a fragment of a phone
    /// number, not a phone number.
    /// </remarks>
    /// <param name="phoneSearchTerm">Raw search term, in any format, possibly <see langword="null"/>.</param>
    /// <returns>Digits-only search term, or an empty string when the term holds no digit.</returns>
    public static string NormalizeSearchTerm(string? phoneSearchTerm)
    {
        return KeepDigits(phoneSearchTerm);
    }

    /// <summary>
    /// Reverses an already normalized phone value.
    /// </summary>
    /// <param name="normalizedPhone">A value already produced by <see cref="Normalize"/> or
    /// <see cref="NormalizeSearchTerm"/>. An empty string is accepted and returns an empty string,
    /// because a search term may legitimately be empty.</param>
    /// <returns><paramref name="normalizedPhone"/> with its digits in reverse order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="normalizedPhone"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="normalizedPhone"/> contains a character that is not a digit, which means the caller skipped normalization.</exception>
    public static string Reverse(string normalizedPhone)
    {
        ArgumentNullException.ThrowIfNull(normalizedPhone);

        if (!normalizedPhone.All(char.IsDigit))
        {
            throw new ArgumentException(
                "Phone must be normalized to digits before it is reversed.",
                nameof(normalizedPhone));
        }

        var reversedPhone = normalizedPhone.ToCharArray();
        Array.Reverse(reversedPhone);
        return new string(reversedPhone);
    }

    /// <summary>
    /// Reports whether a raw phone number holds the digit count required of stored business data.
    /// </summary>
    /// <remarks>
    /// Lets a request validator surface a readable field error before the aggregate throws.
    /// <see cref="Normalize"/> remains the correctness boundary; this is only a pre-check.
    /// </remarks>
    /// <param name="phone">Raw phone number, in any format, possibly <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the value contains 9 to 15 digits; otherwise <see langword="false"/>.</returns>
    public static bool HasPersistableDigitCount(string? phone)
    {
        var digitCount = KeepDigits(phone).Length;
        return digitCount is >= MinimumDigitCount and <= MaximumDigitCount;
    }

    private static string KeepDigits(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        return new string(phone.Where(char.IsDigit).ToArray());
    }
}
