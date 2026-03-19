using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Extensions;
using EntityFrameworkCore.ManagedViews.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Extensions;

public sealed class ModelBuilderExtensionsTests
{
    private static ModelBuilder CreateModelBuilder()
    {
        return new ModelBuilder();
    }

    [Fact]
    public void HasManagedView_AddsDefinitionToAnnotations()
    {
        var mb = CreateModelBuilder();

        mb.HasManagedView("vw_test", v => v.AsSql("SELECT 1"));

        var annotation = mb.Model.FindAnnotation(ManagedViewsAnnotationNames.ViewDefinitions);
        annotation.Should().NotBeNull();
        var defs = annotation!.Value as List<ManagedViewDefinition>;
        defs.Should().ContainSingle();
        defs![0].ViewName.Should().Be("vw_test");
    }

    [Fact]
    public void HasManagedView_WithSchema_SetsSchema()
    {
        var mb = CreateModelBuilder();

        mb.HasManagedView("vw_test", v => v.InSchema("catalog").AsSql("SELECT 1"));

        var defs = mb.Model.FindAnnotation(ManagedViewsAnnotationNames.ViewDefinitions)!.Value as List<ManagedViewDefinition>;
        defs![0].Schema.Should().Be("catalog");
    }

    [Fact]
    public void HasManagedView_WithoutSchema_DefaultsToPublic()
    {
        var mb = CreateModelBuilder();

        mb.HasManagedView("vw_test", v => v.AsSql("SELECT 1"));

        var defs = mb.Model.FindAnnotation(ManagedViewsAnnotationNames.ViewDefinitions)!.Value as List<ManagedViewDefinition>;
        defs![0].Schema.Should().Be("public");
    }

    [Fact]
    public void HasManagedView_AsMaterialized_SetsViewType()
    {
        var mb = CreateModelBuilder();

        mb.HasManagedView("mv_test", v => v.AsMaterialized().AsSql("SELECT 1"));

        var defs = mb.Model.FindAnnotation(ManagedViewsAnnotationNames.ViewDefinitions)!.Value as List<ManagedViewDefinition>;
        defs![0].ViewType.Should().Be(ManagedViewType.Materialized);
    }

    [Fact]
    public void HasManagedView_WithDependencies_SetsDependsOn()
    {
        var mb = CreateModelBuilder();

        mb.HasManagedView("mv_test", v => v.DependsOn("vw_base").AsSql("SELECT 1"));

        var defs = mb.Model.FindAnnotation(ManagedViewsAnnotationNames.ViewDefinitions)!.Value as List<ManagedViewDefinition>;
        defs![0].DependsOn.Should().Contain("vw_base");
    }

    [Fact]
    public void HasManagedView_WithIndexes_SetsIndexes()
    {
        var mb = CreateModelBuilder();

        mb.HasManagedView("mv_test", v => v
            .AsMaterialized()
            .HasIndex("idx_col", "col1")
            .AsSql("SELECT 1"));

        var defs = mb.Model.FindAnnotation(ManagedViewsAnnotationNames.ViewDefinitions)!.Value as List<ManagedViewDefinition>;
        defs![0].Indexes.Should().ContainSingle();
        defs[0].Indexes[0].Name.Should().Be("idx_col");
    }

    [Fact]
    public void HasManagedView_MultipleViews_AddsAll()
    {
        var mb = CreateModelBuilder();

        mb.HasManagedView("vw_a", v => v.AsSql("SELECT 1"));
        mb.HasManagedView("vw_b", v => v.AsSql("SELECT 2"));

        var defs = mb.Model.FindAnnotation(ManagedViewsAnnotationNames.ViewDefinitions)!.Value as List<ManagedViewDefinition>;
        defs.Should().HaveCount(2);
    }

    [Fact]
    public void HasManagedView_WithoutSql_ThrowsInvalidOperationException()
    {
        var mb = CreateModelBuilder();

        var act = () => mb.HasManagedView("vw_test", _ => { });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have SQL defined*");
    }

    [Fact]
    public void HasManagedView_NullBuilder_ThrowsArgumentNullException()
    {
        ModelBuilder mb = null!;
        var act = () => mb.HasManagedView("vw_test", v => v.AsSql("SELECT 1"));
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HasManagedView_NullViewName_ThrowsArgumentException()
    {
        var mb = CreateModelBuilder();
        var act = () => mb.HasManagedView(null!, v => v.AsSql("SELECT 1"));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HasManagedView_EmptyViewName_ThrowsArgumentException()
    {
        var mb = CreateModelBuilder();
        var act = () => mb.HasManagedView("", v => v.AsSql("SELECT 1"));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HasManagedView_NullConfigure_ThrowsArgumentNullException()
    {
        var mb = CreateModelBuilder();
        var act = () => mb.HasManagedView("vw_test", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HasManagedView_ReturnsSameBuilder()
    {
        var mb = CreateModelBuilder();
        var result = mb.HasManagedView("vw_test", v => v.AsSql("SELECT 1"));
        result.Should().BeSameAs(mb);
    }

    [Fact]
    public void HasManagedView_SetsSourceToFluentApi()
    {
        var mb = CreateModelBuilder();

        mb.HasManagedView("vw_test", v => v.AsSql("SELECT 1"));

        var defs = mb.Model.FindAnnotation(ManagedViewsAnnotationNames.ViewDefinitions)!.Value as List<ManagedViewDefinition>;
        defs![0].Source.Should().StartWith("FluentApi:");
    }
}
