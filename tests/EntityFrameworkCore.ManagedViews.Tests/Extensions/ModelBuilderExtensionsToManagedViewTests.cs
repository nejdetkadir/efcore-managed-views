using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Extensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Extensions;

public sealed class ModelBuilderExtensionsToManagedViewTests
{
#pragma warning disable CA1812
    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    private sealed class TestContext : DbContext
    {
        private readonly Action<ModelBuilder>? _configure;
        public TestContext(DbContextOptions options, Action<ModelBuilder>? configure = null) : base(options)
        {
            _configure = configure;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _configure?.Invoke(modelBuilder);
        }
    }
#pragma warning restore CA1812

    [Fact]
    public void ToManagedView_SetsAnnotations()
    {
        var opts = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        using var context = new TestContext(opts, mb =>
        {
            mb.Entity<TestEntity>(b =>
            {
                b.HasNoKey();
                b.ToManagedView("vw_test_entity", "public");
            });
        });

        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        entityType.Should().NotBeNull();

        var isManagedView = entityType!.FindAnnotation(ManagedViewsAnnotationNames.IsManagedView);
        isManagedView.Should().NotBeNull();
        isManagedView!.Value.Should().Be(true);

        var viewName = entityType.FindAnnotation(ManagedViewsAnnotationNames.ManagedViewName);
        viewName.Should().NotBeNull();
        viewName!.Value.Should().Be("vw_test_entity");
    }

    [Fact]
    public void ToManagedView_MapsToView()
    {
        var opts = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        using var context = new TestContext(opts, mb =>
        {
            mb.Entity<TestEntity>(b =>
            {
                b.HasNoKey();
                b.ToManagedView("vw_test_entity");
            });
        });

        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        entityType!.GetViewName().Should().Be("vw_test_entity");
    }

    [Fact]
    public void ToManagedView_WithSchema_SetsAnnotation()
    {
        var opts = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        using var context = new TestContext(opts, mb =>
        {
            mb.Entity<TestEntity>(b =>
            {
                b.HasNoKey();
                b.ToManagedView("vw_test_entity", "catalog");
            });
        });

        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        var managedViewName = entityType!.FindAnnotation(ManagedViewsAnnotationNames.ManagedViewName);
        managedViewName!.Value.Should().Be("vw_test_entity");
    }

    [Fact]
    public void ToManagedView_NullBuilder_ThrowsArgumentNullException()
    {
        EntityTypeBuilder<TestEntity> builder = null!;
        var act = () => builder.ToManagedView("vw_test");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToManagedView_NullViewName_ThrowsArgumentException()
    {
        var opts = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        using var context = new TestContext(opts, mb =>
        {
            mb.Entity<TestEntity>(b =>
            {
                var act = () => b.ToManagedView(null!);
                act.Should().Throw<ArgumentException>();
            });
        });
        _ = context.Model;
    }

    [Fact]
    public void ToManagedView_EmptyViewName_ThrowsArgumentException()
    {
        var opts = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        using var context = new TestContext(opts, mb =>
        {
            mb.Entity<TestEntity>(b =>
            {
                var act = () => b.ToManagedView("");
                act.Should().Throw<ArgumentException>();
            });
        });
        _ = context.Model;
    }

    [Fact]
    public void ToManagedView_WithoutSchema_StillSetsManagedViewAnnotation()
    {
        var opts = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        using var context = new TestContext(opts, mb =>
        {
            mb.Entity<TestEntity>(b =>
            {
                b.HasNoKey();
                b.ToManagedView("vw_test_entity");
            });
        });

        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        var isManagedView = entityType!.FindAnnotation(ManagedViewsAnnotationNames.IsManagedView);
        isManagedView!.Value.Should().Be(true);
    }
}
