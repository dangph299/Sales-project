namespace AuditLog.Tests;

internal static class ReliabilityTestSettings
{
    public static bool Enabled => string.Equals(Environment.GetEnvironmentVariable("RUN_RELIABILITY_TESTS"), "true", StringComparison.OrdinalIgnoreCase);
    public static string MongoConnectionString => Environment.GetEnvironmentVariable("MONGO_TEST_CONNECTION") ?? "mongodb://localhost:27017";
    public static string MongoDatabase => Environment.GetEnvironmentVariable("MONGO_TEST_DATABASE") ?? "audit_reliability_tests";
}
