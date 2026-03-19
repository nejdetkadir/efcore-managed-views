# Test Agent Instructions

## Test Projects

| Project | Type | DB Required |
|---------|------|-------------|
| EntityFrameworkCore.ManagedViews.Tests | Unit | No |
| EntityFrameworkCore.ManagedViews.PostgreSQL.Tests | Unit | No |
| EntityFrameworkCore.ManagedViews.IntegrationTests | Integration | Yes (Docker) |
| EntityFrameworkCore.ManagedViews.FunctionalTests | Functional | No |

## Writing Tests

- Name: `MethodUnderTest_Scenario_ExpectedResult`
- Use `[Fact]` for single cases, `[Theory]` + `[InlineData]` for parameterized
- Use `FluentAssertions` (.Should()) for all assertions
- Use `NSubstitute` for mocking interfaces
- Integration tests use `Testcontainers.PostgreSql` to spin up PostgreSQL in Docker

## Coverage

The project maintains 100% line and method coverage. Every new code path must have a corresponding test.

## Running Tests

```bash
dotnet test                                                    # All tests
dotnet test tests/EntityFrameworkCore.ManagedViews.Tests        # Unit only
dotnet test tests/EntityFrameworkCore.ManagedViews.IntegrationTests  # Integration
```
