namespace Sales.Infrastructure;

/// <summary>
/// Binds a business code prefix to the PostgreSQL sequence that allocates its numbers.
/// </summary>
/// <remarks>
/// The prefix and the sequence name are declared here once and nowhere else: the EF model reads
/// these to create the sequences, the migration reads them to seed each one, and the generators
/// read them to build codes.
/// </remarks>
public sealed record EntityCodeSequence
{
    private EntityCodeSequence(string prefix, string sequenceName)
    {
        Prefix = prefix;
        SequenceName = sequenceName;
    }

    /// <summary>Gets the sequence that numbers customer codes.</summary>
    public static EntityCodeSequence Customer { get; } = new("CUS", "customer_code_seq");

    /// <summary>Gets the sequence that numbers product codes.</summary>
    public static EntityCodeSequence Product { get; } = new("PRD", "product_code_seq");

    /// <summary>Gets the sequence that numbers category codes.</summary>
    public static EntityCodeSequence Category { get; } = new("CAT", "category_code_seq");

    /// <summary>Gets the sequence that numbers order codes.</summary>
    public static EntityCodeSequence Order { get; } = new("ORD", "order_code_seq");

    /// <summary>Gets the literal that every code of this kind starts with.</summary>
    public string Prefix { get; }

    /// <summary>Gets the name of the PostgreSQL sequence backing this code.</summary>
    public string SequenceName { get; }

    /// <summary>Gets every sequence the Sales database owns.</summary>
    public static IReadOnlyList<EntityCodeSequence> All { get; } = [Customer, Product, Category, Order];
}
