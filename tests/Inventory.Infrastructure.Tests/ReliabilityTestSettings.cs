namespace Inventory.Infrastructure.Tests;

internal static class ReliabilityTestSettings
{
    public static bool Enabled => string.Equals(Environment.GetEnvironmentVariable("RUN_RELIABILITY_TESTS"), "true", StringComparison.OrdinalIgnoreCase);

    public static string InventoryPostgresConnectionString =>
        Environment.GetEnvironmentVariable("INVENTORY_TEST_POSTGRES") ??
        "Host=localhost;Port=5432;Database=inventory_reliability_tests;Username=postgres;Password=postgres";
}
