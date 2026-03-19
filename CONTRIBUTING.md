# Contributing to EntityFrameworkCore.ManagedViews

Thank you for your interest in contributing. This guide covers everything you need to get started.

## Table of Contents

- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [Making Changes](#making-changes)
- [Testing](#testing)
- [Pull Request Process](#pull-request-process)
- [Coding Standards](#coding-standards)
- [Reporting Issues](#reporting-issues)

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/<your-username>/efcore-managed-views.git`
3. Create a feature branch: `git checkout -b feature/my-feature`
4. Make your changes
5. Push to your fork: `git push origin feature/my-feature`
6. Open a pull request

## Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/get-started) (for integration tests with PostgreSQL via Testcontainers)
- A code editor (VS Code, Rider, Visual Studio)

### Build

```bash
dotnet restore
dotnet build
```

### Run Tests

```bash
# Unit tests only (no database required)
dotnet test tests/EntityFrameworkCore.ManagedViews.Tests
dotnet test tests/EntityFrameworkCore.ManagedViews.PostgreSQL.Tests

# Integration tests (requires Docker for Testcontainers)
dotnet test tests/EntityFrameworkCore.ManagedViews.IntegrationTests

# Functional tests
dotnet test tests/EntityFrameworkCore.ManagedViews.FunctionalTests

# All tests
dotnet test
```

### Code Coverage

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage-results
reportgenerator \
  -reports:"coverage-results/**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:TextSummary
cat coverage-report/Summary.txt
```

The project maintains 100% line coverage and 100% method coverage.

## Project Structure

```
ManagedViews/
├── src/
│   ├── EntityFrameworkCore.ManagedViews/           # Core package
│   │   ├── Abstractions/                           # Interfaces (IManagedViewProvider, etc.)
│   │   ├── Configuration/                          # ManagedViewOptions, builder, extension
│   │   ├── DependencyResolution/                   # Topological sort resolver
│   │   ├── Diffing/                                # Snapshot differ
│   │   ├── Discovery/                              # SQL file + fluent API discovery
│   │   ├── Exceptions/                             # Custom exception hierarchy
│   │   ├── Extensions/                             # DbContext, ModelBuilder, DI extensions
│   │   ├── Hashing/                                # SHA256 hasher, SQL normalizer
│   │   ├── Migrations/                             # Design-time services, operations
│   │   ├── Models/                                 # DTOs (definition, diff, drift, history)
│   │   ├── Snapshot/                               # JSON snapshot store
│   │   └── Tracking/                               # History repository
│   └── EntityFrameworkCore.ManagedViews.PostgreSQL/ # PostgreSQL provider
│       └── Extensions/                             # Npgsql integration
├── tests/
│   ├── EntityFrameworkCore.ManagedViews.Tests/              # Core unit tests
│   ├── EntityFrameworkCore.ManagedViews.PostgreSQL.Tests/    # PostgreSQL unit tests
│   ├── EntityFrameworkCore.ManagedViews.IntegrationTests/    # Integration tests (Testcontainers)
│   └── EntityFrameworkCore.ManagedViews.FunctionalTests/     # End-to-end pipeline tests
├── samples/
│   └── Sample.ECommerce/                           # Example application
├── docs/                                           # Additional documentation
├── Directory.Build.props                           # Shared build properties
└── global.json                                     # SDK version
```

For a detailed breakdown of each component, see the [Architecture Guide](docs/ARCHITECTURE.md).

## Making Changes

### Adding a Feature

1. Check existing [issues](https://github.com/nejdetkadir/efcore-managed-views/issues) to see if it has been discussed
2. Open an issue describing the feature before writing code
3. Reference the issue in your PR

### Fixing a Bug

1. Open an issue with a minimal reproduction if one does not exist
2. Write a failing test that demonstrates the bug
3. Fix the bug and ensure all tests pass

### Adding a Database Provider

If you want to add support for a new database (e.g., SQL Server, MySQL), see the [Provider Guide](docs/PROVIDERS.md) for the interface contract and implementation pattern.

## Testing

### Test Categories

| Project | Type | Database Required |
|---------|------|-------------------|
| `EntityFrameworkCore.ManagedViews.Tests` | Unit | No |
| `EntityFrameworkCore.ManagedViews.PostgreSQL.Tests` | Unit | No |
| `EntityFrameworkCore.ManagedViews.FunctionalTests` | Functional | No |
| `EntityFrameworkCore.ManagedViews.IntegrationTests` | Integration | Yes (Docker) |

### Test Expectations

- All new code must have tests
- The project maintains 100% line coverage -- do not introduce uncovered lines
- Use `FluentAssertions` for all assertions
- Use `NSubstitute` for mocking interfaces
- Integration tests use [Testcontainers](https://dotnet.testcontainers.org/) to spin up PostgreSQL

### Writing Tests

- Place unit tests in the matching namespace under the appropriate test project
- Name test methods using: `MethodUnderTest_Scenario_ExpectedResult`
- One assertion concept per test
- Use `[Fact]` for single cases, `[Theory]` with `[InlineData]` for parameterized cases

## Pull Request Process

1. **Ensure all tests pass**: `dotnet test`
2. **Ensure no build warnings**: the project uses `TreatWarningsAsErrors`
3. **Update documentation** if your change affects the public API or configuration
4. **Write a clear PR description** covering what changed and why
5. **Reference any related issues** using `Fixes #123` or `Closes #123`
6. PRs require at least one maintainer review before merging

### PR Title Convention

Use a descriptive title that summarizes the change:

- `feat: add SQL Server provider`
- `fix: handle null schema in view discovery`
- `docs: add migration integration examples`
- `test: cover edge cases in topological sort`
- `refactor: simplify snapshot deserialization`

## Coding Standards

### General

- Follow existing code patterns and conventions
- All public types and members must have XML documentation comments
- Use `ArgumentNullException.ThrowIfNull()` and `ArgumentException.ThrowIfNullOrWhiteSpace()` for parameter validation
- Prefer `sealed` classes unless inheritance is intended
- Avoid `var` when the type is not obvious from the right-hand side

### Analyzer Rules

The project enforces:

- `TreatWarningsAsErrors: true`
- `EnforceCodeStyleInBuild: true`
- `AnalysisLevel: latest-all`
- All .NET analyzers enabled

### Dependencies

- Do not add dependencies to the core package unless absolutely necessary
- Provider packages should only depend on the core package and their database driver
- Test projects may use `FluentAssertions`, `NSubstitute`, `Testcontainers`, and `xunit`

## Reporting Issues

### Bug Reports

Use the [bug report template](.github/ISSUE_TEMPLATE/bug_report.md) and include:

- .NET version and EF Core version
- Database provider and version
- Minimal reproduction steps
- Expected vs actual behavior
- Stack trace (if applicable)

### Feature Requests

Use the [feature request template](.github/ISSUE_TEMPLATE/feature_request.md) and include:

- Problem or use case description
- Proposed solution
- Alternatives you have considered

## Questions?

For questions about using ManagedViews, open a [discussion](https://github.com/nejdetkadir/efcore-managed-views/discussions) rather than an issue.
