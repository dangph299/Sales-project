using Xunit;

namespace AuditLog.Tests;

/// <summary>
/// Shares a single <see cref="MongoReliabilityFixture"/> across the audit reliability tests so the
/// container starts once per test run.
/// </summary>
[CollectionDefinition("AuditReliabilityMongo")]
public sealed class AuditReliabilityMongoCollection : ICollectionFixture<MongoReliabilityFixture>;
