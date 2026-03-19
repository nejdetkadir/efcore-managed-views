# Copilot Instructions

## Project

EntityFrameworkCore.ManagedViews - code-first view management for Entity Framework Core (.NET 10, C#).

## Build and Test

```bash
dotnet build                    # Build
dotnet test                     # All tests
make test-unit                  # Unit tests only
make coverage                   # With coverage
```

## Conventions

- File-scoped namespaces, nullable enabled, TreatWarningsAsErrors
- Sealed classes by default
- Private fields: _camelCase, Interfaces: IPrefix
- XML docs on all public members
- Tests: MethodUnderTest_Scenario_ExpectedResult
- FluentAssertions for assertions, NSubstitute for mocking
- 100% line coverage required
