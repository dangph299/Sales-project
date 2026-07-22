using System.Globalization;

namespace Sales.Infrastructure;

/// <summary>
/// Binds a business code prefix to the PostgreSQL sequence that allocates its numbers, and to the
/// way those numbers are rendered into a code.
/// </summary>
/// <remarks>
/// The prefix, the sequence name and the code format are declared here once and nowhere else: the
/// EF model reads these to create the sequences and size the columns, the migration reads them to
/// seed each one, and the generators read them to build codes.
/// <para>
/// The formats are not uniform. Customer, product and category codes are padded to three digits but
/// keep counting past 999 into four, so they have no ceiling. Order codes are a fixed-width
/// <c>ORD-0000001</c>, which does have a ceiling — running past it would produce a code that no
/// longer fits the column or the agreed format, so the sequence refuses rather than widening.
/// </para>
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
    /// <param name="sequenceNumber">The number <c>nextval</c> handed out.</param>
    /// <exception cref="InvalidOperationException">
    /// The sequence has run past <see cref="MaximumSequenceNumber"/>. Continuing would hand out a
    /// code wider than the agreed format and the column, so allocation stops here instead.
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
