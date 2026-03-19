using EntityFrameworkCore.ManagedViews.Extensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Extensions;

public sealed class MigrationBuilderExtensionsEdgeCaseTests
{
    [Fact]
    public void CreateManagedView_NullBuilder_ThrowsArgumentNullException()
    {
        MigrationBuilder builder = null!;
        var act = () => builder.CreateManagedView("vw_test", "public", "SELECT 1");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DropManagedView_NullBuilder_ThrowsArgumentNullException()
    {
        MigrationBuilder builder = null!;
        var act = () => builder.DropManagedView("vw_test", "public");
        act.Should().Throw<ArgumentNullException>();
    }
}
