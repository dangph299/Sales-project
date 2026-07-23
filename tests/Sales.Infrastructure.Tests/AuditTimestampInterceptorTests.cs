using BuildingBlocks.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Sales.Infrastructure.Tests;

public sealed class AuditTimestampInterceptorTests
{
    [Fact]
    public async Task Added_entity_gets_created_and_updated_audit_values()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 7, 23, 4, 0, 0, TimeSpan.Zero));
        await using var fixture = await Fixture.CreateAsync(clock);
        await using var context = fixture.CreateContext();

        context.AuditedEntities.Add(new AuditedEntity { Id = Guid.NewGuid(), Name = "Created" });

        await context.SaveChangesAsync();

        var saved = await context.AuditedEntities.AsNoTracking().SingleAsync();
        Assert.Equal(clock.GetUtcNow(), saved.CreatedAt);
        Assert.Equal(clock.GetUtcNow(), saved.UpdatedAt);
        Assert.Equal("tester", saved.CreatedBy);
        Assert.Equal("tester", saved.UpdatedBy);
    }

    [Fact]
    public async Task Modified_entity_updates_only_updated_audit_values()
    {
        var initialNow = new DateTimeOffset(2026, 7, 23, 4, 0, 0, TimeSpan.Zero);
        var changedNow = initialNow.AddMinutes(5);
        var clock = new FixedTimeProvider(initialNow);
        await using var fixture = await Fixture.CreateAsync(clock);
        var id = Guid.NewGuid();

        await using (var seed = fixture.CreateContext())
        {
            seed.AuditedEntities.Add(new AuditedEntity { Id = id, Name = "Before" });
            await seed.SaveChangesAsync();
        }

        clock.UtcNow = changedNow;
        await using (var update = fixture.CreateContext())
        {
            var entity = await update.AuditedEntities.SingleAsync(x => x.Id == id);
            entity.Name = "After";
            await update.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContext();
        var saved = await verify.AuditedEntities.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(initialNow, saved.CreatedAt);
        Assert.Equal(changedNow, saved.UpdatedAt);
        Assert.Equal("tester", saved.UpdatedBy);
    }

    [Fact]
    public async Task No_op_audit_only_change_does_not_touch_updated_at_or_created_at()
    {
        var initialNow = new DateTimeOffset(2026, 7, 23, 4, 0, 0, TimeSpan.Zero);
        var clock = new FixedTimeProvider(initialNow);
        await using var fixture = await Fixture.CreateAsync(clock);
        var id = Guid.NewGuid();

        await using (var seed = fixture.CreateContext())
        {
            seed.AuditedEntities.Add(new AuditedEntity { Id = id, Name = "Stable" });
            await seed.SaveChangesAsync();
        }

        clock.UtcNow = initialNow.AddMinutes(10);
        await using (var update = fixture.CreateContext())
        {
            var entity = await update.AuditedEntities.SingleAsync(x => x.Id == id);
            entity.CreatedAt = clock.GetUtcNow();
            entity.UpdatedAt = clock.GetUtcNow();
            await update.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContext();
        var saved = await verify.AuditedEntities.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(initialNow, saved.CreatedAt);
        Assert.Equal(initialNow, saved.UpdatedAt);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class StaticAuditContextAccessor : IAuditContextAccessor
    {
        public string? ActorId => "tester";
        public string? ActorName => "Tester";
        public string? CorrelationId => null;
        public string? CausationId => null;
        public string? TraceId => null;
    }

    private sealed class AuditedEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public long Version { get; set; }
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<AuditedEntity> AuditedEntities => Set<AuditedEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditedEntity>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(64);
                entity.Property(x => x.CreatedBy).HasMaxLength(64);
                entity.Property(x => x.UpdatedBy).HasMaxLength(64);
                entity.Property(x => x.Version).IsConcurrencyToken();
            });
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly DbContextOptions<TestDbContext> options;

        private Fixture(SqliteConnection connection, DbContextOptions<TestDbContext> options)
        {
            this.connection = connection;
            this.options = options;
        }

        public static async Task<Fixture> CreateAsync(TimeProvider clock)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var interceptor = new AuditTimestampSaveChangesInterceptor(clock, new StaticAuditContextAccessor());
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(interceptor)
                .Options;
            var fixture = new Fixture(connection, options);
            await using var context = fixture.CreateContext();
            await context.Database.EnsureCreatedAsync();
            return fixture;
        }

        public TestDbContext CreateContext() => new(options);

        public ValueTask DisposeAsync() => connection.DisposeAsync();
    }
}
