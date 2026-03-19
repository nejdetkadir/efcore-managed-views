# Tech Stack

## Runtime

| Component | Technology | Version |
|-----------|-----------|---------|
| Language | C# | Latest (LangVersion: latest) |
| Runtime | .NET | 10.0 |
| ORM | Entity Framework Core | 10.x |
| Database | PostgreSQL | 16+ |
| DB Driver | Npgsql.EntityFrameworkCore.PostgreSQL | 10.x |

## Build and Tooling

| Component | Technology |
|-----------|-----------|
| Build System | MSBuild / dotnet CLI |
| SDK Version | 10.0.100 (pinned in global.json) |
| Package Format | NuGet (.nupkg + .snupkg symbols) |
| Code Analysis | .NET Analyzers (latest-all) |
| Code Style | .editorconfig with enforced rules |
| Warnings | TreatWarningsAsErrors: true |

## Testing

| Component | Technology | Purpose |
|-----------|-----------|---------|
| Test Framework | xUnit | Test runner and assertions |
| Assertions | FluentAssertions | Fluent readable assertions |
| Mocking | NSubstitute | Interface mocking |
| Containers | Testcontainers.PostgreSql | PostgreSQL in Docker for integration tests |
| Coverage | XPlat Code Coverage (Coverlet) | Line and branch coverage collection |
| Reporting | ReportGenerator | Coverage report generation |

## CI/CD

| Component | Technology |
|-----------|-----------|
| CI | GitHub Actions |
| Test DB | PostgreSQL 16-alpine (Docker) |
| Package Registry | NuGet.org |

## Project Constraints

- No unnecessary dependencies in the core package
- Provider packages depend only on core plus their database driver
- All public APIs must have XML documentation
- 100% line coverage and 100% method coverage required
- File-scoped namespaces enforced
- Nullable reference types enabled
