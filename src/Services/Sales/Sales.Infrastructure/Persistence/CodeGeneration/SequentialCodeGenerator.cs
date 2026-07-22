using Microsoft.EntityFrameworkCore;

namespace Sales.Infrastructure;

/// <summary>
/// Allocates sequential business codes from PostgreSQL sequences.
/// </summary>
/// <remarks>
/// The database is the single source of truth for the next number. <c>nextval</c> is atomic and
/// never returns the same value twice, so concurrent creates across any number of API instances
/// each receive a distinct code without locking. Existing codes in the business tables are never
/// scanned; they only seed the sequences once, in the migration that creates them.
/// <para>
/// Codes are unique and monotonically increasing. Gap-free sequencing is not guaranteed: a sequence
/// does not roll back, so a number allocated by a create that later fails is simply skipped.
/// </para>
/// </remarks>
public sealed class SequentialCodeGenerator(SalesDbContext db)
{
    /// <summary>
    /// Allocates the next code for one sequence.
    /// </summary>
    /// <param name="codeSequence">Prefix and backing sequence to allocate from.</param>
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
