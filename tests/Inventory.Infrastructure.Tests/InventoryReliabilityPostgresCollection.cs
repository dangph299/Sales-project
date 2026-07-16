using Xunit;

namespace Inventory.Infrastructure.Tests;

/// <summary>
/// Shares a single <see cref="InventoryPostgresReliabilityFixture"/> across the Inventory reliability
/// tests so the container starts once per test run.
/// </summary>
[CollectionDefinition("InventoryReliabilityPostgres")]
public sealed class InventoryReliabilityPostgresCollection : ICollectionFixture<InventoryPostgresReliabilityFixture>;
