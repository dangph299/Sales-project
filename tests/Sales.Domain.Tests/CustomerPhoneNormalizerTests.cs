namespace Sales.Domain.Tests;

public sealed class CustomerPhoneNormalizerTests
{
    [Theory]
    [InlineData("090 123 4567", "0901234567")]
    [InlineData("0901-234-567", "0901234567")]
    [InlineData("090.123.4567", "0901234567")]
    [InlineData("  0901234567  ", "0901234567")]
    [InlineData("(+84) 901 234 567", "84901234567")]
    public void Normalize_strips_every_non_digit(string customerPhone, string expectedNormalizedCustomerPhone)
    {
        Assert.Equal(expectedNormalizedCustomerPhone, CustomerPhoneNormalizer.Normalize(customerPhone));
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("090-123-4")]
    public void Normalize_rejects_fewer_than_nine_digits(string customerPhone)
    {
        Assert.Throws<DomainException>(() => CustomerPhoneNormalizer.Normalize(customerPhone));
    }

    [Theory]
    [InlineData("1234567890123456")]
    [InlineData("0901 2345 6789 0123")]
    public void Normalize_rejects_more_than_fifteen_digits(string customerPhone)
    {
        Assert.Throws<DomainException>(() => CustomerPhoneNormalizer.Normalize(customerPhone));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Normalize_rejects_a_missing_value(string? customerPhone)
    {
        Assert.Throws<DomainException>(() => CustomerPhoneNormalizer.Normalize(customerPhone!));
    }

    [Theory]
    [InlineData("4567", "4567")]
    [InlineData("090-1", "0901")]
    [InlineData("0901234567", "0901234567")]
    public void NormalizeSearchTerm_allows_a_value_shorter_than_a_persisted_phone(
        string customerPhoneSearchTerm,
        string expectedNormalizedCustomerPhoneSearchTerm)
    {
        Assert.Equal(
            expectedNormalizedCustomerPhoneSearchTerm,
            CustomerPhoneNormalizer.NormalizeSearchTerm(customerPhoneSearchTerm));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("---")]
    public void NormalizeSearchTerm_returns_empty_when_the_term_holds_no_digit(string? customerPhoneSearchTerm)
    {
        Assert.Equal(string.Empty, CustomerPhoneNormalizer.NormalizeSearchTerm(customerPhoneSearchTerm));
    }

    [Theory]
    [InlineData("0901234567", "7654321090")]
    [InlineData("4567", "7654")]
    [InlineData("", "")]
    public void Reverse_reverses_an_already_normalized_phone(
        string normalizedCustomerPhone,
        string expectedReversedCustomerPhone)
    {
        Assert.Equal(expectedReversedCustomerPhone, CustomerPhoneNormalizer.Reverse(normalizedCustomerPhone));
    }

    [Theory]
    [InlineData("0901-234-567")]
    [InlineData("090 123 4567")]
    public void Reverse_rejects_a_value_that_was_not_normalized_first(string normalizedCustomerPhone)
    {
        Assert.Throws<ArgumentException>(() => CustomerPhoneNormalizer.Reverse(normalizedCustomerPhone));
    }

    [Theory]
    [InlineData("0901234567", true)]
    [InlineData("090 123 4567", true)]
    [InlineData("12345678", false)]
    [InlineData("1234567890123456", false)]
    [InlineData(null, false)]
    public void HasPersistableDigitCount_reports_whether_a_phone_can_be_stored(
        string? customerPhone,
        bool expectedResult)
    {
        Assert.Equal(expectedResult, CustomerPhoneNormalizer.HasPersistableDigitCount(customerPhone));
    }
}
