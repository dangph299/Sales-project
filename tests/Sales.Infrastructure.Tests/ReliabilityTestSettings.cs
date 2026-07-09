namespace Sales.Infrastructure.Tests;

internal static class ReliabilityTestSettings
{
    public static bool Enabled => string.Equals(Environment.GetEnvironmentVariable("RUN_RELIABILITY_TESTS"), "true", StringComparison.OrdinalIgnoreCase);

    public static string SalesPostgresConnectionString =>
        Environment.GetEnvironmentVariable("SALES_TEST_POSTGRES") ??
        "Host=localhost;Port=5432;Database=sales_reliability_tests;Username=postgres;Password=postgres";
}
