# C# Instructions

When writing C# code in this project:

- Use file-scoped namespaces
- Enable nullable reference types
- Prefer sealed classes
- Use ArgumentNullException.ThrowIfNull() for parameter validation
- Use ArgumentException.ThrowIfNullOrWhiteSpace() for string parameters
- Add XML documentation on all public types and members
- Private fields: _camelCase prefix
- Register services via IDbContextOptionsExtension.ApplyServices

When writing tests:
- Name: MethodUnderTest_Scenario_ExpectedResult
- Use FluentAssertions (.Should())
- Use NSubstitute for mocking
- Use [Fact] for single cases, [Theory] + [InlineData] for parameterized
