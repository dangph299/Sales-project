namespace Sales.Domain;

/// <summary>
/// An immutable monetary amount in VND, always rounded to whole units and never negative.
/// </summary>
public readonly record struct Money
{
    /// <summary>
    /// Gets the rounded, non-negative amount in VND.
    /// </summary>
    public decimal Amount { get; }

    private Money(decimal amount) => Amount = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Creates a <see cref="Money"/> value in VND, rounding to the nearest whole unit.
    /// </summary>
    /// <param name="amount">
    /// The amount to represent. Must not be negative.
    /// </param>
    /// <returns>
    /// The rounded monetary value.
    /// </returns>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="amount"/> is negative.
    /// </exception>
    public static Money Vnd(decimal amount)
    {
        if (amount < 0) throw new DomainException("Money cannot be negative.");
        return new Money(amount);
    }

    /// <summary>
    /// Adds two monetary amounts.
    /// </summary>
    /// <param name="left">
    /// The first amount.
    /// </param>
    /// <param name="right">
    /// The second amount.
    /// </param>
    /// <returns>
    /// The sum of <paramref name="left"/> and <paramref name="right"/>.
    /// </returns>
    public static Money operator +(Money left, Money right) => Vnd(left.Amount + right.Amount);
}
