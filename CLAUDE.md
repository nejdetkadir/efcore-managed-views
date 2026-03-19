# CLAUDE.md

See AGENTS.md for complete project instructions including build commands, test commands, code conventions, project structure, and architecture.

## Quick Reference

```bash
dotnet build                    # Build
dotnet test                     # All tests
make test-unit                  # Unit tests only  
make coverage                   # With coverage
make lint                       # Lint check
```

## Key Rules

- .NET 10, C# latest, file-scoped namespaces, nullable enabled
- TreatWarningsAsErrors: true, all analyzers at latest-all
- Sealed classes by default, XML docs on all public members
- Tests: FluentAssertions + NSubstitute, 100% coverage required
- Test naming: MethodUnderTest_Scenario_ExpectedResult
