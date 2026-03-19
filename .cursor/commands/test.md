# Generate Tests

Write comprehensive tests for the current file or selected code.

## Steps

1. Identify the test project:
   - `src/EntityFrameworkCore.ManagedViews/` -> `tests/EntityFrameworkCore.ManagedViews.Tests/`
   - `src/EntityFrameworkCore.ManagedViews.PostgreSQL/` -> `tests/EntityFrameworkCore.ManagedViews.PostgreSQL.Tests/`
2. Check existing test patterns in the target test project
3. Create test class in matching namespace directory

## Test Requirements

- Framework: xUnit
- Assertions: FluentAssertions (`.Should()`)
- Mocking: NSubstitute
- Naming: `MethodUnderTest_Scenario_ExpectedResult`
- Use `[Fact]` for single cases, `[Theory]` + `[InlineData]` for parameterized
- Cover: happy path, edge cases, null inputs, error conditions
- Achieve 100% line coverage of the code under test

## Example

```csharp
public sealed class Sha256ViewHasherTests
{
    private readonly Sha256ViewHasher _sut = new();

    [Fact]
    public void ComputeHash_ValidSql_ReturnsConsistentHash()
    {
        var hash1 = _sut.ComputeHash("SELECT 1");
        var hash2 = _sut.ComputeHash("SELECT 1");
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHash_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.ComputeHash(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
```
