# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| 1.x     | Yes       |

## Reporting a Vulnerability

If you discover a security vulnerability in EntityFrameworkCore.ManagedViews, please report it responsibly.

**Do not open a public GitHub issue for security vulnerabilities.**

Instead, please send an email to the project maintainer with:

- A description of the vulnerability
- Steps to reproduce
- The potential impact
- Any suggested fixes (if you have them)

### What to Expect

- **Acknowledgment** within 48 hours of your report
- **Assessment** within 1 week, including severity and affected versions
- **Fix and disclosure** coordinated with you before public announcement

### Scope

This policy covers the following packages:

- `EntityFrameworkCore.ManagedViews`
- `EntityFrameworkCore.ManagedViews.PostgreSQL`

### Out of Scope

- Vulnerabilities in Entity Framework Core itself (report to [Microsoft](https://msrc.microsoft.com/))
- Vulnerabilities in Npgsql (report to the [Npgsql project](https://github.com/npgsql/npgsql/security))
- Vulnerabilities in PostgreSQL (report to the [PostgreSQL project](https://www.postgresql.org/support/security/))

## Security Considerations

### SQL Injection

ManagedViews generates DDL statements using view definitions provided by the developer (via embedded SQL files or the fluent API). The library **does not** accept end-user input in SQL generation paths. However:

- View SQL content is embedded directly into `CREATE VIEW` statements. Ensure your `.sql` files are authored by trusted developers and reviewed in pull requests.
- The `__ManagedViewsHistory` tracking table uses parameterized queries for all data operations.

### Tracking Table

The `__ManagedViewsHistory` table stores view names, schemas, hashes, and timestamps. It does not store sensitive data. The table name and schema are configurable via `ManagedViewOptions`.
