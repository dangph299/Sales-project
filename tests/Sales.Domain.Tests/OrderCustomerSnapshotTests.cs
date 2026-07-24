namespace Sales.Domain.Tests;

public sealed class OrderCustomerSnapshotTests
{
    [Theory]
    [InlineData("090 123 4567", "090 123 4567", "0901234567", "7654321090")]
    [InlineData("0901-234-567", "0901-234-567", "0901234567", "7654321090")]
    [InlineData("  (+84) 901 234 567  ", "(+84) 901 234 567", "84901234567", "76543210948")]
    public void OrderCustomerPhone_derives_all_three_values_from_one_input(
        string customerPhone,
        string expectedDisplayValue,
        string expectedNormalizedValue,
        string expectedReversedValue)
    {
        var orderCustomerPhone = OrderCustomerPhone.Create(customerPhone);

        Assert.Equal(expectedDisplayValue, orderCustomerPhone.DisplayValue);
        Assert.Equal(expectedNormalizedValue, orderCustomerPhone.NormalizedValue);
        Assert.Equal(expectedReversedValue, orderCustomerPhone.ReversedValue);
    }

    [Fact]
    public void OrderCustomerPhone_reversed_value_always_matches_its_normalized_value()
    {
        var orderCustomerPhone = OrderCustomerPhone.Create("0912-345-678");

        Assert.Equal(
            PhoneNumberNormalizer.Reverse(orderCustomerPhone.NormalizedValue),
            orderCustomerPhone.ReversedValue);
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("")]
    [InlineData("   ")]
    public void OrderCustomerPhone_rejects_a_phone_that_cannot_be_stored(string customerPhone)
    {
        Assert.Throws<DomainException>(() => OrderCustomerPhone.Create(customerPhone));
    }

    [Fact]
    public void Create_keeps_the_display_phone_and_derives_the_search_values()
    {
        var customerId = Guid.NewGuid();

        var orderCustomerSnapshot = OrderCustomerSnapshot.Create(
            customerId,
            "  Nguyen Van A  ",
            "0901-234-567",
            "customer@example.com",
            "12 Le Loi");

        Assert.Equal(customerId, orderCustomerSnapshot.CustomerId);
        Assert.Equal("Nguyen Van A", orderCustomerSnapshot.Name);
        Assert.Equal("0901-234-567", orderCustomerSnapshot.Phone.DisplayValue);
        Assert.Equal("0901234567", orderCustomerSnapshot.Phone.NormalizedValue);
        Assert.Equal("7654321090", orderCustomerSnapshot.Phone.ReversedValue);
        Assert.Equal("customer@example.com", orderCustomerSnapshot.Email);
        Assert.Equal("12 Le Loi", orderCustomerSnapshot.Address);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_missing_customer_name(string customerName)
    {
        Assert.Throws<DomainException>(() => OrderCustomerSnapshot.Create(
            Guid.NewGuid(),
            customerName,
            "0901234567",
            null,
            null));
    }

    [Fact]
    public void Create_rejects_an_empty_customer_id()
    {
        Assert.Throws<DomainException>(() => OrderCustomerSnapshot.Create(
            Guid.Empty,
            "Nguyen Van A",
            "0901234567",
            null,
            null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_stores_a_blank_email_or_address_as_null(string? blankValue)
    {
        var orderCustomerSnapshot = OrderCustomerSnapshot.Create(
            Guid.NewGuid(),
            "Nguyen Van A",
            "0901234567",
            blankValue,
            blankValue);

        Assert.Null(orderCustomerSnapshot.Email);
        Assert.Null(orderCustomerSnapshot.Address);
    }

    [Fact]
    public void Create_trims_the_email_and_address()
    {
        var orderCustomerSnapshot = OrderCustomerSnapshot.Create(
            Guid.NewGuid(),
            "Nguyen Van A",
            "0901234567",
            "  customer@example.com  ",
            "  12 Le Loi  ");

        Assert.Equal("customer@example.com", orderCustomerSnapshot.Email);
        Assert.Equal("12 Le Loi", orderCustomerSnapshot.Address);
    }
}
