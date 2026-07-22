using Xunit;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Pins the prefix-to-sequence pairing. A copy/paste slip here would silently allocate product
/// numbers from the customer sequence, which no amount of concurrency testing would reveal because
/// each sequence is internally consistent on its own.
/// </summary>
public sealed class EntityCodeSequenceTests
{
    [Fact]
    public void Customer_codes_come_from_the_customer_sequence()
    {
        Assert.Equal("CUS", EntityCodeSequence.Customer.Prefix);
        Assert.Equal("customer_code_seq", EntityCodeSequence.Customer.SequenceName);
    }

    [Fact]
    public void Product_codes_come_from_the_product_sequence()
    {
        Assert.Equal("PRD", EntityCodeSequence.Product.Prefix);
        Assert.Equal("product_code_seq", EntityCodeSequence.Product.SequenceName);
    }

    [Fact]
    public void Category_codes_come_from_the_category_sequence()
    {
        Assert.Equal("CAT", EntityCodeSequence.Category.Prefix);
        Assert.Equal("category_code_seq", EntityCodeSequence.Category.SequenceName);
    }

    [Fact]
    public void Order_codes_come_from_the_order_sequence()
    {
        Assert.Equal("ORD-", EntityCodeSequence.Order.Prefix);
        Assert.Equal("order_code_seq", EntityCodeSequence.Order.SequenceName);
    }

    [Theory]
    [InlineData(1, "ORD-0000001")]
    [InlineData(2, "ORD-0000002")]
    [InlineData(533, "ORD-0000533")]
    [InlineData(9_999_999, "ORD-9999999")]
    public void Order_codes_are_the_prefix_and_seven_digits(long sequenceNumber, string expectedOrderCode)
    {
        Assert.Equal(expectedOrderCode, EntityCodeSequence.Order.FormatCode(sequenceNumber));
    }

    [Fact]
    public void Order_code_at_the_ceiling_still_fits_eleven_characters()
    {
        // The orders.OrderCode column is sized from the prefix and the width, so a format change
        // that outgrew the column would have to come through here first.
        Assert.Equal(11, EntityCodeSequence.Order.FormatCode(9_999_999).Length);
        Assert.Equal(
            11,
            EntityCodeSequence.Order.Prefix.Length + EntityCodeSequence.Order.NumericWidth);
    }

    [Theory]
    [InlineData(10_000_000)]
    [InlineData(99_999_999)]
    public void Order_sequence_past_its_ceiling_fails_instead_of_widening_the_code(long sequenceNumber)
    {
        var failure = Assert.Throws<InvalidOperationException>(
            () => EntityCodeSequence.Order.FormatCode(sequenceNumber));

        // Silently producing ORD-10000000 would break the agreed format and overflow the column;
        // the message has to name the sequence so an operator knows which one ran out.
        Assert.Contains("order_code_seq", failure.Message, StringComparison.Ordinal);
        Assert.Contains("9999999", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Customer_product_and_category_codes_keep_counting_past_three_digits()
    {
        // These have no agreed ceiling, and existing data relies on CUS1000 following CUS999.
        Assert.Equal("CUS001", EntityCodeSequence.Customer.FormatCode(1));
        Assert.Equal("PRD1000", EntityCodeSequence.Product.FormatCode(1000));
        Assert.Equal("CAT123456", EntityCodeSequence.Category.FormatCode(123456));
    }

    [Fact]
    public void No_prefix_or_sequence_is_shared_between_entities()
    {
        var prefixes = EntityCodeSequence.All.Select(sequence => sequence.Prefix).ToArray();
        var sequenceNames = EntityCodeSequence.All.Select(sequence => sequence.SequenceName).ToArray();

        Assert.Equal(4, EntityCodeSequence.All.Count);
        Assert.Equal(prefixes.Length, prefixes.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(sequenceNames.Length, sequenceNames.Distinct(StringComparer.Ordinal).Count());
    }
}
