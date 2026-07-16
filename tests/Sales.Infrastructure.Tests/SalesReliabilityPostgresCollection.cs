using Xunit;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Shares a single <see cref="PostgresReliabilityFixture"/> across the Sales reliability tests so the
/// container starts once per test run.
/// </summary>
[CollectionDefinition("SalesReliabilityPostgres")]
public sealed class SalesReliabilityPostgresCollection : ICollectionFixture<PostgresReliabilityFixture>;
