---
name: testing-coverage
description: >
  Manages test coverage for EntityFrameworkCore.ManagedViews.
  Use when writing tests, checking coverage, finding uncovered lines,
  or when asked to achieve a coverage target. Covers unit tests,
  integration tests with Testcontainers, and code coverage tooling.
---

# Testing & Coverage Management

## Running Tests

```bash
# All tests
dotnet test

# Unit tests (no Docker needed)
dotnet test tests/EntityFrameworkCore.ManagedViews.Tests
dotnet test tests/EntityFrameworkCore.ManagedViews.PostgreSQL.Tests

# Integration tests (Docker required)
dotnet test tests/EntityFrameworkCore.ManagedViews.IntegrationTests

# Functional tests
dotnet test tests/EntityFrameworkCore.ManagedViews.FunctionalTests
```

## Measuring Coverage

```bash
# Collect coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage-results

# Generate readable report
reportgenerator \
  -reports:"coverage-results/**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:TextSummary

# View results
cat coverage-report/Summary.txt
```

## Coverage Target

**100% line coverage, 100% method coverage** -- no exceptions.

## Finding Uncovered Code

1. Generate HTML report: `-reporttypes:Html`
2. Open `coverage-report/index.html`
3. Navigate to uncovered files
4. Identify uncovered branches and lines

## Common Patterns for Hard-to-Cover Code

### Mocking Assembly for embedded resource tests
```csharp
var mockAssembly = Substitute.For<Assembly>();
mockAssembly.GetManifestResourceNames().Returns(new[] { "Ns.Views.test.sql" });
mockAssembly.GetManifestResourceStream(Arg.Any<string>()).Returns((Stream?)null);
```

### Testing EF Core internal DI
```csharp
var builder = new DbContextOptionsBuilder<DbContext>()
    .UseNpgsql(connectionString)
    .UseNpgsqlManagedViews()
    .EnableServiceProviderCaching(false);
```

### Covering exception branches
```csharp
[Fact]
public void Method_InvalidInput_ThrowsSpecificException()
{
    var act = () => sut.Method(invalidInput);
    act.Should().Throw<ManagedViewProviderException>()
       .WithMessage("*expected message*");
}
```

## Test File Placement

| Source | Test Project | Test Location |
|--------|-------------|--------------|
| `src/.../Discovery/SqlFileViewDiscoveryService.cs` | `.Tests` | `Discovery/SqlFileViewDiscoveryServiceTests.cs` |
| `src/.../PostgreSQL/PostgreSqlManagedViewProvider.cs` | `.PostgreSQL.Tests` | `PostgreSqlManagedViewProviderTests.cs` |
| `src/.../Extensions/DbContextExtensions.cs` | `.IntegrationTests` | `DbContextExtensionsFullTests.cs` |
