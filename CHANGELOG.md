# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Core package `EntityFrameworkCore.ManagedViews` with view discovery, hashing, diffing, snapshot, and dependency resolution
- PostgreSQL provider `EntityFrameworkCore.ManagedViews.PostgreSQL` with materialized view support
- SQL file discovery with metadata directives (`@viewName`, `@schema`, `@type`, `@dependsOn`, `@indexes`)
- Fluent API discovery via `ModelBuilder.HasManagedView()`
- Entity mapping via `EntityTypeBuilder.ToManagedView()`
- SHA256-based change detection with SQL normalization (whitespace, comments, semicolons)
- JSON snapshot store for design-time change tracking
- `__ManagedViewsHistory` tracking table for runtime drift detection
- Topological sort dependency resolver with cycle detection
- Custom migration operations: `CreateManagedViewOperation`, `DropManagedViewOperation`, `RefreshMaterializedViewOperation`
- PostgreSQL DDL generation for views, materialized views, and indexes
- `PostgreSqlManagedViewMigrationsSqlGenerator` extending Npgsql's migration generator
- Runtime extensions: `RefreshMaterializedViewAsync`, `RefreshAllMaterializedViewsAsync`, `CheckViewDriftAsync`
- `ManagedViewDesignTimeServices` for EF Core design-time integration
- Custom exception hierarchy: `ManagedViewException`, `ManagedViewSqlParseException`, `ManagedViewProviderException`, `ManagedViewDependencyCycleException`, `ManagedViewDuplicateException`, `ManagedViewNotFoundException`
- Full service registration via `ManagedViewOptionsExtension.ApplyServices` and `IServiceCollection.AddManagedViews`
- CI workflow with PostgreSQL integration tests
- Release workflow with NuGet publishing
- 100% line coverage, 100% method coverage across all source assemblies
- Comprehensive documentation: README, Implementation Guide, Architecture, Provider Guide, Contributing, Security Policy
