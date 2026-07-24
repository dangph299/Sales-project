namespace Sales.Domain;

/// <summary>
/// The three forms an order stores for one customer phone number.
/// </summary>
/// <remarks>
/// All three are derived from a single input and are read-only, so they cannot disagree with each
/// other.
/// </remarks>
public sealed record OrderCustomerPhone
{
    private OrderCustomerPhone(string displayValue, string normalizedValue, string reversedValue)
    {
        DisplayValue = displayValue;
        NormalizedValue = normalizedValue;
        ReversedValue = reversedValue;
    }

    /// <summary>
    /// Gets the phone number as the user entered it, trimmed but otherwise unchanged, so an order
    /// shows back the formatting it was given.
    /// </summary>
    public string DisplayValue { get; }

    /// <summary>
    /// Gets the digits-only phone number, which exact and prefix searches match against.
    /// </summary>
    public string NormalizedValue { get; }

    /// <summary>
    /// Gets <see cref="NormalizedValue"/> with its digits reversed, which is what a search matches
    /// against when it matches the end of a phone number.
    /// </summary>
    public string ReversedValue { get; }

    /// <summary>
    /// Creates the three phone forms from one raw phone number.
    /// </summary>
    /// <param name="customerPhone">Raw phone number, in any format containing 9 to 15 digits.</param>
    /// <returns>Validated phone value object.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="customerPhone"/> is empty or whitespace, or does not contain 9 to 15 digits.</exception>
    public static OrderCustomerPhone Create(string customerPhone)
    {
        var normalizedCustomerPhone = PhoneNumberNormalizer.Normalize(customerPhone);

        return new OrderCustomerPhone(
            customerPhone.Trim(),
            normalizedCustomerPhone,
            PhoneNumberNormalizer.Reverse(normalizedCustomerPhone));
    }
}
