using System.Reflection;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Discovery;
using EntityFrameworkCore.ManagedViews.Models;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Discovery;

public sealed class SqlFileViewDiscoveryServiceEmbeddedTests
{
    private readonly SqlFileViewDiscoveryService _sut = new();

    [Fact]
    public void Discover_WithEmbeddedResources_FindsSqlFiles()
    {
        var options = new ManagedViewOptions
        {
            ViewAssembly = typeof(SqlFileViewDiscoveryServiceEmbeddedTests).Assembly,
            SqlFilesPrefix = "Views"
        };

        var result = _sut.Discover(options);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void Discover_ViewDirectives_ParsedCorrectly()
    {
        var options = new ManagedViewOptions
        {
            ViewAssembly = typeof(SqlFileViewDiscoveryServiceEmbeddedTests).Assembly,
            SqlFilesPrefix = "Views"
        };

        var result = _sut.Discover(options);

        var productView = result.FirstOrDefault(v => v.ViewName == "vw_test_products");
        productView.Should().NotBeNull();
        productView!.Schema.Should().Be("public");
        productView.ViewType.Should().Be(ManagedViewType.View);
        productView.Sql.Should().Contain("SELECT id, name, price");
    }

    [Fact]
    public void Discover_MaterializedView_ParsedCorrectly()
    {
        var options = new ManagedViewOptions
        {
            ViewAssembly = typeof(SqlFileViewDiscoveryServiceEmbeddedTests).Assembly,
            SqlFilesPrefix = "Views"
        };

        var result = _sut.Discover(options);

        var matView = result.FirstOrDefault(v => v.ViewType == ManagedViewType.Materialized);
        matView.Should().NotBeNull();
        matView!.DependsOn.Should().Contain("vw_test_products");
        matView.Indexes.Should().ContainSingle();
        matView.Indexes[0].Name.Should().Be("idx_category");
    }

    [Fact]
    public void Discover_CustomPrefix_FiltersCorrectly()
    {
        var options = new ManagedViewOptions
        {
            ViewAssembly = typeof(SqlFileViewDiscoveryServiceEmbeddedTests).Assembly,
            SqlFilesPrefix = "ZZZ_ThisPrefixDoesNotExist"
        };

        var result = _sut.Discover(options);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Discover_CaseInsensitivePrefix()
    {
        var options = new ManagedViewOptions
        {
            ViewAssembly = typeof(SqlFileViewDiscoveryServiceEmbeddedTests).Assembly,
            SqlFilesPrefix = "views"
        };

        var result = _sut.Discover(options);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void Discover_AssemblyWithNoSqlResources_ReturnsEmpty()
    {
        var options = new ManagedViewOptions
        {
            ViewAssembly = typeof(object).Assembly,
            SqlFilesPrefix = "Views"
        };

        var result = _sut.Discover(options);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Discover_NullViewAssembly_UsesEntryOrCallingAssembly()
    {
        var options = new ManagedViewOptions
        {
            ViewAssembly = null,
            SqlFilesPrefix = "NonExistent_XYZ_Prefix"
        };

        var result = _sut.Discover(options);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Discover_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => _sut.Discover(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Discover_SetsSources()
    {
        var options = new ManagedViewOptions
        {
            ViewAssembly = typeof(SqlFileViewDiscoveryServiceEmbeddedTests).Assembly,
            SqlFilesPrefix = "Views"
        };

        var result = _sut.Discover(options);

        result.Should().OnlyContain(d => !string.IsNullOrEmpty(d.Source));
    }

    [Fact]
    public void Discover_DefinitionsHaveCorrectSql()
    {
        var options = new ManagedViewOptions
        {
            ViewAssembly = typeof(SqlFileViewDiscoveryServiceEmbeddedTests).Assembly,
            SqlFilesPrefix = "Views"
        };

        var result = _sut.Discover(options);

        result.Should().OnlyContain(d => !string.IsNullOrEmpty(d.Sql));
    }

    [Fact]
    public void Discover_CustomDefaultSchema_AppliedWhenNoDirective()
    {
        var options = new ManagedViewOptions
        {
            ViewAssembly = typeof(SqlFileViewDiscoveryServiceEmbeddedTests).Assembly,
            SqlFilesPrefix = "Views",
            DefaultSchema = "custom_default"
        };

        var result = _sut.Discover(options);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void Discover_StreamReturnsNull_SkipsResource()
    {
        var mockAssembly = Substitute.For<Assembly>();
        mockAssembly.GetManifestResourceNames().Returns(["Views.test_null_stream.sql"]);
        mockAssembly.GetManifestResourceStream("Views.test_null_stream.sql").Returns((Stream?)null);

        var options = new ManagedViewOptions
        {
            ViewAssembly = mockAssembly,
            SqlFilesPrefix = "Views"
        };

        var result = _sut.Discover(options);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Discover_MixedNullAndValidStreams_OnlyReturnsValid()
    {
        var validContent = "-- @viewName: vw_valid\nSELECT 1"u8.ToArray();
        var validStream = new MemoryStream(validContent);

        var mockAssembly = Substitute.For<Assembly>();
        mockAssembly.GetManifestResourceNames().Returns([
            "Views.null_stream.sql",
            "Views.valid_stream.sql"
        ]);
        mockAssembly.GetManifestResourceStream("Views.null_stream.sql").Returns((Stream?)null);
        mockAssembly.GetManifestResourceStream("Views.valid_stream.sql").Returns(validStream);

        var options = new ManagedViewOptions
        {
            ViewAssembly = mockAssembly,
            SqlFilesPrefix = "Views"
        };

        var result = _sut.Discover(options);
        result.Should().ContainSingle();
        result[0].ViewName.Should().Be("vw_valid");
    }

    [Fact]
    public void ExtractFileName_NormalResource_ExtractsFileName()
    {
        var result = SqlFileViewDiscoveryService.ExtractFileName("MyApp.Views.my_view.sql");
        result.Should().Be("my_view.sql");
    }

    [Fact]
    public void ExtractFileName_SchemaQualified_ExtractsLastPart()
    {
        var result = SqlFileViewDiscoveryService.ExtractFileName("MyApp.Views.catalog.mv_stats.sql");
        result.Should().Be("mv_stats.sql");
    }

    [Fact]
    public void ExtractFileName_NoDotBeforeSql_ReturnsFullName()
    {
        var result = SqlFileViewDiscoveryService.ExtractFileName("a.sql");
        result.Should().Be("a.sql");
    }

    [Fact]
    public void ExtractFileName_NoDotsAtAll_ReturnsFullName()
    {
        var result = SqlFileViewDiscoveryService.ExtractFileName("viewsql");
        result.Should().Be("viewsql");
    }

    [Fact]
    public void ExtractFileName_SingleDotAtStart_ReturnsRightPart()
    {
        var result = SqlFileViewDiscoveryService.ExtractFileName("namespace.view.sql");
        result.Should().Be("view.sql");
    }
}
