namespace Sales.Application.Features.Customers.Enums;

/// <summary>
/// How a phone-number search filter should match against stored customer phone numbers.
/// </summary>
public enum PhoneMatch
{
    /// <summary>Match phone numbers that start with the search value.</summary>
    Prefix = 1,

    /// <summary>Match phone numbers that end with the search value.</summary>
    Suffix = 2
}
