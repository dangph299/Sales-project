using Sales.Application;

namespace Sales.Infrastructure.Tests;

internal sealed class TestExecutionContext : IExecutionContext
{
    public string Actor => "test";
    public Guid CorrelationId => Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
}
