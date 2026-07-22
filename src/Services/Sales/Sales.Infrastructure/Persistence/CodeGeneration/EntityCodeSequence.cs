using System.Globalization;

namespace Sales.Infrastructure;

/// <summary>
/// Binds a business code prefix to the PostgreSQL sequence that allocates its numbers, and to the
/// way those numbers are rendered into a code.
/// </summary>
/// <remarks>
/// Customer, product and category codes pad to three digits and keep counting past 999, so they
/// have no ceiling. Order codes are a fixed-width <c>ORD-0000001</c> and stop at
/// <c>ORD-9999999</c>.
/// </remarks>
public sealed record EntityCodeSequence
{
    private EntityCodeSequence(string prefix, string sequenceName, int numericWidth, long? maximumSequenceNumber)
    {
        Prefix = prefix;
        SequenceName = sequenceName;
        NumericWidth = numericWidth;
        MaximumSequenceNumber = maximumSequenceNumber;
    }

    /// <summary>Gets the sequence that numbers customer codes, as CUS001.</summary>
    public static EntityCodeSequence Customer { get; } = new("CUS", "customer_code_seq", 3, null);

    /// <summary>Gets the sequence that numbers product codes, as PRD001.</summary>
    public static EntityCodeSequence Product { get; } = new("PRD", "product_code_seq", 3, null);

    /// <summary>Gets the sequence that numbers category codes, as CAT001.</summary>
    public static EntityCodeSequence Category { get; } = new("CAT", "category_code_seq", 3, null);

    /// <summary>Gets the sequence that numbers order codes, as ORD-0000001 through ORD-9999999.</summary>
    public static EntityCodeSequence Order { get; } = new("ORD-", "order_code_seq", 7, 9_999_999);

    /// <summary>Gets the literal that every code of this kind starts with.</summary>
    public string Prefix { get; }

    /// <summary>Gets the name of the PostgreSQL sequence backing this code.</summary>
    public string SequenceName { get; }

    /// <summary>Gets the number of digits a sequence number is left-padded to.</summary>
    public int NumericWidth { get; }

    /// <summary>
    /// Gets the highest number this sequence may render, or <see langword="null"/> when numbers
    /// wider than <see cref="NumericWidth"/> are allowed to keep all their digits.
    /// </summary>
    public long? MaximumSequenceNumber { get; }

    /// <summary>Gets every sequence the Sales database owns.</summary>
    public static IReadOnlyList<EntityCodeSequence> All { get; } = [Customer, Product, Category, Order];

    /// <summary>
    /// Renders one allocated sequence number as a business code.
    /// </summary>
    /// <param name="sequenceNumber">The allocated number.</param>
    /// <returns>The prefix followed by the number, padded to <see cref="NumericWidth"/> digits.</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="sequenceNumber"/> is past <see cref="MaximumSequenceNumber"/>.
    /// </exception>
    public string FormatCode(long sequenceNumber)
    {
        if (sequenceNumber > MaximumSequenceNumber)
        {
            throw new InvalidOperationException(
                $"Sequence '{SequenceName}' has reached {sequenceNumber}, past its last usable number "
                + $"{MaximumSequenceNumber}. Codes are formatted as {Prefix} plus {NumericWidth} digits and "
                + "cannot be widened without changing the code format and the column width.");
        }

        return Prefix + sequenceNumber.ToString(
            "D" + NumericWidth.ToString(CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture);
    }
}
