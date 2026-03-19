using EntityFrameworkCore.ManagedViews.Configuration;
using FluentAssertions;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Configuration;

public sealed class ManagedViewBuilderEdgeCaseTests
{
    [Fact]
    public void InSchema_NullSchema_ThrowsArgumentException()
    {
        var builder = new ManagedViewBuilder();
        var act = () => builder.InSchema(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InSchema_EmptySchema_ThrowsArgumentException()
    {
        var builder = new ManagedViewBuilder();
        var act = () => builder.InSchema("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InSchema_WhitespaceSchema_ThrowsArgumentException()
    {
        var builder = new ManagedViewBuilder();
        var act = () => builder.InSchema("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AsSql_NullSql_ThrowsArgumentException()
    {
        var builder = new ManagedViewBuilder();
        var act = () => builder.AsSql(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AsSql_EmptySql_ThrowsArgumentException()
    {
        var builder = new ManagedViewBuilder();
        var act = () => builder.AsSql("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DependsOn_NullViewName_ThrowsArgumentException()
    {
        var builder = new ManagedViewBuilder();
        var act = () => builder.DependsOn((string)null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DependsOn_EmptyViewName_ThrowsArgumentException()
    {
        var builder = new ManagedViewBuilder();
        var act = () => builder.DependsOn("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DependsOn_NullArray_ThrowsArgumentNullException()
    {
        var builder = new ManagedViewBuilder();
        var act = () => builder.DependsOn((string[])null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HasIndex_NullIndexName_ThrowsArgumentException()
    {
        var builder = new ManagedViewBuilder();
        var act = () => builder.HasIndex(null!, "col");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HasIndex_EmptyColumns_ThrowsArgumentException()
    {
        var builder = new ManagedViewBuilder();
        var act = () => builder.HasIndex("idx", "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FluentChaining_AllMethodsReturnSameBuilder()
    {
        var builder = new ManagedViewBuilder();

        var result = builder
            .InSchema("public")
            .AsSql("SELECT 1")
            .AsMaterialized()
            .DependsOn("base")
            .HasIndex("idx", "col");

        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void MultipleIndexes_CanBeAdded()
    {
        var builder = new ManagedViewBuilder();

        builder
            .HasIndex("idx_a", "col_a")
            .HasIndex("idx_b", "col_b")
            .HasIndex("idx_c", "col_c");

        builder.IndexDefinitions.Should().HaveCount(3);
    }

    [Fact]
    public void Default_ViewType_IsView()
    {
        var builder = new ManagedViewBuilder();
        builder.ViewTypeValue.Should().Be(EntityFrameworkCore.ManagedViews.Models.ManagedViewType.View);
    }

    [Fact]
    public void Default_Schema_IsNull()
    {
        var builder = new ManagedViewBuilder();
        builder.SchemaValue.Should().BeNull();
    }

    [Fact]
    public void Default_Sql_IsNull()
    {
        var builder = new ManagedViewBuilder();
        builder.SqlValue.Should().BeNull();
    }

    [Fact]
    public void Default_Dependencies_IsEmpty()
    {
        var builder = new ManagedViewBuilder();
        builder.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void Default_Indexes_IsEmpty()
    {
        var builder = new ManagedViewBuilder();
        builder.IndexDefinitions.Should().BeEmpty();
    }
}
