using System.Runtime.CompilerServices;

namespace Sales.Architecture.Tests;

/// <summary>
/// Guards against Inventory messaging jobs regressing back to PostgreSQL advisory locks. Sales'
/// own jobs still use <c>pg_try_advisory_xact_lock</c> via the shared Hangfire job bases, so this
/// scans only the Inventory job sources rather than the shared base classes.
/// </summary>
public sealed class InventoryMessagingJobsPostgresAdvisoryLockRemovalTests
{
    [Fact]
    public void Inventory_messaging_jobs_do_not_reference_postgres_advisory_locks()
    {
        var jobsDirectory = GetInventoryJobsDirectory();
        var jobFiles = Directory.GetFiles(jobsDirectory, "*.cs");

        Assert.NotEmpty(jobFiles);
        foreach (var file in jobFiles)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("pg_try_advisory_xact_lock", content);
            Assert.DoesNotContain("InventoryMessagingJobLockKeys", content);
        }
    }

    private static string GetInventoryJobsDirectory([CallerFilePath] string testFilePath = "")
    {
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFilePath)!, "..", ".."));
        return Path.Combine(
            repoRoot,
            "src", "Services", "Inventory", "Inventory.Infrastructure", "Hangfire", "Jobs");
    }
}
