namespace Sales.Domain;

/// <summary>
/// Which end of an order's customer phone number a search term must match.
/// </summary>
/// <remarks>
/// Stated explicitly by the caller rather than guessed from the term's length or shape: "4567" is
/// an equally plausible prefix and suffix, and guessing wrong silently returns the wrong orders.
/// </remarks>
public enum OrderCustomerPhoneMatchMode
{
    /// <summary>The phone number starts with the search term.</summary>
    Prefix = 1,

    /// <summary>The phone number ends with the search term.</summary>
    Suffix = 2
}
