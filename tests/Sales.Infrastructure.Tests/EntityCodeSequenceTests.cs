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
        Assert.Equal("ORD", EntityCodeSequence.Order.Prefix);
        Assert.Equal("order_code_seq", EntityCodeSequence.Order.SequenceName);
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
