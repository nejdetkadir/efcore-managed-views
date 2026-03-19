using EntityFrameworkCore.ManagedViews.Migrations.Operations;
using EntityFrameworkCore.ManagedViews.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.PostgreSQL.Tests;

public sealed class PostgreSqlManagedViewMigrationsSqlGeneratorTests
{
    private readonly PostgreSqlManagedViewProvider _viewProvider = new();

    private PostgreSqlManagedViewMigrationsSqlGenerator CreateGenerator()
    {
        var optionsBuilder = new DbContextOptionsBuilder()
            .UseNpgsql("Host=localhost;Database=test;Username=test;Password=test");
        using var context = new DbContext(optionsBuilder.Options);

        var dependencies = context.GetService<MigrationsSqlGeneratorDependencies>();

#pragma warning disable EF1001
        var npgsqlOptions = context.GetService<Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal.INpgsqlSingletonOptions>();
#pragma warning restore EF1001
        context.Dispose();

        return new PostgreSqlManagedViewMigrationsSqlGenerator(dependencies, npgsqlOptions, _viewProvider);
    }

    [Fact]
    public void Generate_CreateViewOperation_GeneratesCreateOrReplaceSql()
    {
        var generator = CreateGenerator();
        var operations = new List<MigrationOperation>
        {
            new CreateManagedViewOperation
            {
                ViewName = "vw_test",
                Schema = "public",
                Sql = "SELECT 1 AS id",
                ViewType = ManagedViewType.View
            }
        };

        var commands = generator.Generate(operations);

        commands.Should().NotBeEmpty();
        var sql = string.Join("\n", commands.Select(c => c.CommandText));
        sql.Should().Contain("CREATE OR REPLACE VIEW");
        sql.Should().Contain("\"public\".\"vw_test\"");
        sql.Should().Contain("SELECT 1 AS id");
    }

    [Fact]
    public void Generate_CreateMaterializedViewOperation_GeneratesDropThenCreate()
    {
        var generator = CreateGenerator();
        var operations = new List<MigrationOperation>
        {
            new CreateManagedViewOperation
            {
                ViewName = "mv_test",
                Schema = "public",
                Sql = "SELECT 1 AS id",
                ViewType = ManagedViewType.Materialized
            }
        };

        var commands = generator.Generate(operations);

        commands.Should().HaveCountGreaterThanOrEqualTo(2);
        var sql = string.Join("\n", commands.Select(c => c.CommandText));
        sql.Should().Contain("DROP MATERIALIZED VIEW IF EXISTS");
        sql.Should().Contain("CREATE MATERIALIZED VIEW IF NOT EXISTS");
        sql.Should().Contain("WITH DATA");
    }

    [Fact]
    public void Generate_CreateMaterializedViewWithIndexes_GeneratesIndexSql()
    {
        var generator = CreateGenerator();
        var operations = new List<MigrationOperation>
        {
            new CreateManagedViewOperation
            {
                ViewName = "mv_test",
                Schema = "public",
                Sql = "SELECT 1 AS id",
                ViewType = ManagedViewType.Materialized,
                Indexes = [new ManagedViewIndex("idx_id", "id")]
            }
        };

        var commands = generator.Generate(operations);

        var sql = string.Join("\n", commands.Select(c => c.CommandText));
        sql.Should().Contain("CREATE INDEX IF NOT EXISTS");
        sql.Should().Contain("\"idx_id\"");
    }

    [Fact]
    public void Generate_DropViewOperation_GeneratesDropSql()
    {
        var generator = CreateGenerator();
        var operations = new List<MigrationOperation>
        {
            new DropManagedViewOperation
            {
                ViewName = "vw_test",
                Schema = "public",
                ViewType = ManagedViewType.View
            }
        };

        var commands = generator.Generate(operations);

        commands.Should().NotBeEmpty();
        var sql = string.Join("\n", commands.Select(c => c.CommandText));
        sql.Should().Contain("DROP VIEW IF EXISTS");
        sql.Should().Contain("CASCADE");
    }

    [Fact]
    public void Generate_DropMaterializedViewOperation_GeneratesDropMaterializedSql()
    {
        var generator = CreateGenerator();
        var operations = new List<MigrationOperation>
        {
            new DropManagedViewOperation
            {
                ViewName = "mv_test",
                Schema = "public",
                ViewType = ManagedViewType.Materialized
            }
        };

        var commands = generator.Generate(operations);

        var sql = string.Join("\n", commands.Select(c => c.CommandText));
        sql.Should().Contain("DROP MATERIALIZED VIEW IF EXISTS");
    }

    [Fact]
    public void Generate_RefreshMaterializedViewOperation_GeneratesRefreshSql()
    {
        var generator = CreateGenerator();
        var operations = new List<MigrationOperation>
        {
            new RefreshMaterializedViewOperation
            {
                ViewName = "mv_test",
                Schema = "public",
                Concurrently = false
            }
        };

        var commands = generator.Generate(operations);

        commands.Should().NotBeEmpty();
        var sql = string.Join("\n", commands.Select(c => c.CommandText));
        sql.Should().Contain("REFRESH MATERIALIZED VIEW");
        sql.Should().Contain("\"public\".\"mv_test\"");
    }

    [Fact]
    public void Generate_RefreshConcurrently_GeneratesConcurrentRefreshSql()
    {
        var generator = CreateGenerator();
        var operations = new List<MigrationOperation>
        {
            new RefreshMaterializedViewOperation
            {
                ViewName = "mv_test",
                Schema = "public",
                Concurrently = true
            }
        };

        var commands = generator.Generate(operations);

        var sql = string.Join("\n", commands.Select(c => c.CommandText));
        sql.Should().Contain("REFRESH MATERIALIZED VIEW CONCURRENTLY");
    }

    [Fact]
    public void Generate_StandardOperation_DelegatesToBase()
    {
        var generator = CreateGenerator();
        var operations = new List<MigrationOperation>
        {
            new SqlOperation { Sql = "SELECT 1" }
        };

        var commands = generator.Generate(operations);

        commands.Should().NotBeEmpty();
        commands[0].CommandText.Should().Contain("SELECT 1");
    }

    [Fact]
    public void Generate_CreateViewNoIndexes_NoIndexCommands()
    {
        var generator = CreateGenerator();
        var operations = new List<MigrationOperation>
        {
            new CreateManagedViewOperation
            {
                ViewName = "vw_no_idx",
                Schema = "public",
                Sql = "SELECT 1 AS id",
                ViewType = ManagedViewType.View,
                Indexes = []
            }
        };

        var commands = generator.Generate(operations);

        commands.Should().ContainSingle();
        var sql = commands[0].CommandText;
        sql.Should().Contain("CREATE OR REPLACE VIEW");
        sql.Should().NotContain("CREATE INDEX");
    }

    [Fact]
    public void Generate_NullBuilder_ThrowsArgumentNullException()
    {
        var generator = CreateGenerator();
        var operations = new List<MigrationOperation>
        {
            new CreateManagedViewOperation
            {
                ViewName = "vw_test",
                Schema = "public",
                Sql = "SELECT 1",
                ViewType = ManagedViewType.View
            }
        };

        var act = () => generator.Generate(operations);
        act.Should().NotThrow();
    }

    [Fact]
    public void Generate_MultipleOperations_GeneratesAllSql()
    {
        var generator = CreateGenerator();
        var operations = new List<MigrationOperation>
        {
            new CreateManagedViewOperation
            {
                ViewName = "vw_base",
                Schema = "public",
                Sql = "SELECT 1",
                ViewType = ManagedViewType.View
            },
            new CreateManagedViewOperation
            {
                ViewName = "mv_derived",
                Schema = "public",
                Sql = "SELECT * FROM vw_base",
                ViewType = ManagedViewType.Materialized
            },
            new DropManagedViewOperation
            {
                ViewName = "vw_old",
                Schema = "public",
                ViewType = ManagedViewType.View
            }
        };

        var commands = generator.Generate(operations);

        commands.Count.Should().BeGreaterThanOrEqualTo(4);
    }
}
