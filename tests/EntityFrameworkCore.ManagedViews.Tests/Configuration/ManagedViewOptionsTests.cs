using EntityFrameworkCore.ManagedViews.Configuration;
using FluentAssertions;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Configuration;

public class ManagedViewOptionsTests
{
    [Fact]
    public void Defaults_ViewAssembly_IsNull()
    {
        var options = new ManagedViewOptions();
        options.ViewAssembly.Should().BeNull();
    }

    [Fact]
    public void Defaults_SqlFilesPrefix_IsViews()
    {
        var options = new ManagedViewOptions();
        options.SqlFilesPrefix.Should().Be("Views");
    }

    [Fact]
    public void Defaults_TrackingTableSchema_IsNull()
    {
        var options = new ManagedViewOptions();
        options.TrackingTableSchema.Should().BeNull();
    }

    [Fact]
    public void Defaults_TrackingTableName_IsManagedViewsHistory()
    {
        var options = new ManagedViewOptions();
        options.TrackingTableName.Should().Be("__ManagedViewsHistory");
    }

    [Fact]
    public void Defaults_AutoCreateTrackingTable_IsTrue()
    {
        var options = new ManagedViewOptions();
        options.AutoCreateTrackingTable.Should().BeTrue();
    }

    [Fact]
    public void Defaults_DefaultSchema_IsPublic()
    {
        var options = new ManagedViewOptions();
        options.DefaultSchema.Should().Be("public");
    }

    [Fact]
    public void Defaults_NormalizeSqlBeforeHashing_IsTrue()
    {
        var options = new ManagedViewOptions();
        options.NormalizeSqlBeforeHashing.Should().BeTrue();
    }

    [Fact]
    public void Defaults_StripCommentsBeforeHashing_IsTrue()
    {
        var options = new ManagedViewOptions();
        options.StripCommentsBeforeHashing.Should().BeTrue();
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        var options = new ManagedViewOptions
        {
            SqlFilesPrefix = "Sql",
            TrackingTableName = "custom_history",
            TrackingTableSchema = "admin",
            AutoCreateTrackingTable = false,
            DefaultSchema = "catalog",
            NormalizeSqlBeforeHashing = false,
            StripCommentsBeforeHashing = false
        };

        options.SqlFilesPrefix.Should().Be("Sql");
        options.TrackingTableName.Should().Be("custom_history");
        options.TrackingTableSchema.Should().Be("admin");
        options.AutoCreateTrackingTable.Should().BeFalse();
        options.DefaultSchema.Should().Be("catalog");
        options.NormalizeSqlBeforeHashing.Should().BeFalse();
        options.StripCommentsBeforeHashing.Should().BeFalse();
    }
}
