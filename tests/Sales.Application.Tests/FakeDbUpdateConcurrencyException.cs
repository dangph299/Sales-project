namespace Microsoft.EntityFrameworkCore;

// Test-only stand-in: declared under the real EF Core namespace so Exception.GetType().FullName
// matches what ErrorLoggingBehavior checks for, without adding a real EF Core package dependency
// to this test project (Sales.Application must not reference Microsoft.EntityFrameworkCore).
internal sealed class DbUpdateConcurrencyException : Exception;
