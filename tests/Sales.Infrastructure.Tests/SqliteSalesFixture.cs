using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sales.Domain;

namespace Sales.Infrastructure.Tests;

internal sealed class SqliteSalesFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<SalesDbContext> _options;
    private readonly TestExecutionContext _executionContext = new();

    private SqliteSalesFixture(SqliteConnection connection, DbContextOptions<SalesDbContext> options)
    {
        _connection = connection;
        _options = options;
    }

    public static async Task<SqliteSalesFixture> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseSqlite(connection)
            .Options;
        var fixture = new SqliteSalesFixture(connection, options);
        await using var context = fixture.CreateContext();
        await context.Database.EnsureCreatedAsync();
        return fixture;
    }

    public SalesDbContext CreateContext()
    {
        return new SalesDbContext(_options, _executionContext);
    }

    public async Task SeedAsync(params Product[] products)
    {
        await using var context = CreateContext();
        foreach (var product in products)
        {
            product.ClearDomainEvents();
        }

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    public async Task SeedAsync(params Customer[] customers)
    {
        await using var context = CreateContext();
        foreach (var customer in customers)
        {
            customer.ClearDomainEvents();
        }

        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
