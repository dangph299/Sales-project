namespace Sales.Application.Tests;

/// <summary>
/// Allocates order codes for tests. Orders carry a unique <c>OrderCode</c>, so a test that persists
/// more than one order needs a distinct code per order rather than a shared literal.
/// </summary>
internal static class OrderTestFactory
{
    private static int orderCodeSequence;

    public static string NextOrderCode()
    {
        return $"ORD{Interlocked.Increment(ref orderCodeSequence):D6}";
    }
}
