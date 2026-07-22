using Microsoft.EntityFrameworkCore;

namespace Sales.Infrastructure;

/// <summary>
/// Allocates sequential business codes from PostgreSQL sequences.
/// </summary>
/// <remarks>
/// Codes are unique and increase over time. They are not gap-free: a number allocated by a create
/// that later fails is skipped rather than reused.
/// </remarks>
public sealed class SequentialCodeGenerator(SalesDbContext db)
{
    /// <summary>
    /// Allocates the next code for one sequence.
    /// </summary>
    /// <param name="codeSequence">Prefix and backing sequence to allocate from.</param>
    /// <returns>The next code for that sequence.</returns>
    /// <exception cref="InvalidOperationException">The sequence has run past its last usable number.</exception>
    public async Task<string> NextCodeAsync(EntityCodeSequence codeSequence, CancellationToken cancellationToken)
    {
        // Parameterised through regclass rather than concatenated into the statement, so the
        // sequence name is never interpolated into SQL text.
        var sequenceNumber = await db.Database
            .SqlQuery<long>($"SELECT nextval({codeSequence.SequenceName}::regclass) AS \"Value\"")
            .SingleAsync(cancellationToken);

        return codeSequence.FormatCode(sequenceNumber);
    }
}
