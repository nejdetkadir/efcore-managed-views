# Testcontainers Setup

## PostgreSQL Integration Test Fixture

```csharp
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
```

## Using the Fixture

```csharp
public sealed class MyIntegrationTest : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public MyIntegrationTest(PostgreSqlFixture fixture) => _fixture = fixture;

    private DbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<DbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .UseNpgsqlManagedViews()
            .EnableServiceProviderCaching(false);
        return new DbContext(builder.Options);
    }

    [Fact]
    public async Task Test_Example()
    {
        using var context = CreateContext();
        // EnableServiceProviderCaching(false) is critical for test isolation
        // ...
    }
}
```

## Prerequisites

- Docker Desktop or Docker Engine running
- NuGet: `Testcontainers.PostgreSql`
- The container starts automatically per test class via `IAsyncLifetime`

## Gotchas

- Always use `EnableServiceProviderCaching(false)` to avoid stale service providers between tests
- Call `repo.EnsureCreatedAsync()` before operations that touch `__ManagedViewsHistory`
- Use `ICurrentDbContext.Context` factory pattern for `IManagedViewHistoryRepository` to avoid circular DI
