using Xunit;

namespace BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// Shares a single <see cref="RedisReliabilityFixture"/> across the distributed lease reliability
/// tests so the container starts once per test run.
/// </summary>
[CollectionDefinition("RedisReliability")]
public sealed class RedisReliabilityCollection : ICollectionFixture<RedisReliabilityFixture>;
