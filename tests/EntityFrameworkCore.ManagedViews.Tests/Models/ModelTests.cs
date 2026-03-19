using EntityFrameworkCore.ManagedViews.Models;
using FluentAssertions;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Models;

public sealed class ModelTests
{
    [Fact]
    public void ManagedViewDefinition_DefaultSchema_IsPublic()
    {
        var def = new ManagedViewDefinition { ViewName = "vw", Sql = "SELECT 1", Source = "test" };
        def.Schema.Should().Be("public");
    }

    [Fact]
    public void ManagedViewDefinition_DefaultViewType_IsView()
    {
        var def = new ManagedViewDefinition { ViewName = "vw", Sql = "SELECT 1", Source = "test" };
        def.ViewType.Should().Be(ManagedViewType.View);
    }

    [Fact]
    public void ManagedViewDefinition_DefaultDependsOn_IsEmpty()
    {
        var def = new ManagedViewDefinition { ViewName = "vw", Sql = "SELECT 1", Source = "test" };
        def.DependsOn.Should().BeEmpty();
    }

    [Fact]
    public void ManagedViewDefinition_DefaultIndexes_IsEmpty()
    {
        var def = new ManagedViewDefinition { ViewName = "vw", Sql = "SELECT 1", Source = "test" };
        def.Indexes.Should().BeEmpty();
    }

    [Fact]
    public void ManagedViewIndex_RecordEquality()
    {
        var idx1 = new ManagedViewIndex("idx_a", "col_a");
        var idx2 = new ManagedViewIndex("idx_a", "col_a");
        idx1.Should().Be(idx2);
    }

    [Fact]
    public void ManagedViewIndex_RecordInequality()
    {
        var idx1 = new ManagedViewIndex("idx_a", "col_a");
        var idx2 = new ManagedViewIndex("idx_b", "col_b");
        idx1.Should().NotBe(idx2);
    }

    [Fact]
    public void ManagedViewDiff_RequiredProperties()
    {
        var def = new ManagedViewDefinition { ViewName = "vw", Sql = "SELECT 1", Source = "test" };
        var diff = new ManagedViewDiff
        {
            Definition = def,
            DiffType = ManagedViewDiffType.Modified,
            PreviousHash = "old",
            CurrentHash = "new"
        };

        diff.Definition.Should().BeSameAs(def);
        diff.DiffType.Should().Be(ManagedViewDiffType.Modified);
        diff.PreviousHash.Should().Be("old");
        diff.CurrentHash.Should().Be("new");
    }

    [Fact]
    public void ViewDriftReport_NoDrift_HasDriftIsFalse()
    {
        var report = new ViewDriftReport { Entries = [] };
        report.HasDrift.Should().BeFalse();
    }

    [Fact]
    public void ViewDriftReport_WithEntries_HasDriftIsTrue()
    {
        var report = new ViewDriftReport
        {
            Entries =
            [
                new ViewDriftEntry
                {
                    ViewName = "vw_test",
                    Schema = "public",
                    DriftType = ViewDriftType.Missing
                }
            ]
        };
        report.HasDrift.Should().BeTrue();
    }

    [Fact]
    public void ViewDriftEntry_AllDriftTypes()
    {
        var missing = new ViewDriftEntry
        {
            ViewName = "vw", Schema = "public",
            DriftType = ViewDriftType.Missing, ExpectedHash = "h1"
        };
        var modified = new ViewDriftEntry
        {
            ViewName = "vw", Schema = "public",
            DriftType = ViewDriftType.Modified, ExpectedHash = "h1", ActualHash = "h2"
        };
        var untracked = new ViewDriftEntry
        {
            ViewName = "vw", Schema = "public",
            DriftType = ViewDriftType.Untracked, ActualHash = "h3"
        };

        missing.DriftType.Should().Be(ViewDriftType.Missing);
        modified.DriftType.Should().Be(ViewDriftType.Modified);
        untracked.DriftType.Should().Be(ViewDriftType.Untracked);
    }

    [Fact]
    public void ManagedViewHistoryEntry_AllProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var entry = new ManagedViewHistoryEntry
        {
            ViewName = "vw_test",
            Schema = "public",
            Hash = "abc",
            ViewType = "View",
            AppliedAt = now
        };

        entry.ViewName.Should().Be("vw_test");
        entry.Schema.Should().Be("public");
        entry.Hash.Should().Be("abc");
        entry.ViewType.Should().Be("View");
        entry.AppliedAt.Should().Be(now);
    }

    [Fact]
    public void ManagedViewSnapshot_Defaults()
    {
        var snap = new EntityFrameworkCore.ManagedViews.Snapshot.ManagedViewSnapshot();
        snap.Version.Should().Be("1.0.0");
        snap.Views.Should().BeEmpty();
        snap.GeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void ManagedViewDiffType_AllValues()
    {
        Enum.GetValues<ManagedViewDiffType>().Should().HaveCount(3);
    }

    [Fact]
    public void ManagedViewType_AllValues()
    {
        Enum.GetValues<ManagedViewType>().Should().HaveCount(2);
    }

    [Fact]
    public void ViewDriftType_AllValues()
    {
        Enum.GetValues<ViewDriftType>().Should().HaveCount(3);
    }
}
