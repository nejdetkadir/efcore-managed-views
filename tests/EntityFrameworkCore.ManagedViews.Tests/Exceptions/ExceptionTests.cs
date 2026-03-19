using EntityFrameworkCore.ManagedViews.Exceptions;
using FluentAssertions;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Exceptions;

public sealed class ExceptionTests
{
    [Fact]
    public void ManagedViewException_DefaultConstructor_HasMessage()
    {
        var ex = new ManagedViewException();
        ex.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ManagedViewException_MessageConstructor_SetsMessage()
    {
        var ex = new ManagedViewException("test error");
        ex.Message.Should().Be("test error");
    }

    [Fact]
    public void ManagedViewException_InnerExceptionConstructor_SetsInner()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new ManagedViewException("outer", inner);
        ex.Message.Should().Be("outer");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void ManagedViewException_IsException()
    {
        new ManagedViewException().Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void ManagedViewSqlParseException_DefaultConstructor()
    {
        var ex = new ManagedViewSqlParseException();
        ex.Should().BeAssignableTo<ManagedViewException>();
        ex.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ManagedViewSqlParseException_MessageConstructor()
    {
        var ex = new ManagedViewSqlParseException("bad sql");
        ex.Message.Should().Be("bad sql");
    }

    [Fact]
    public void ManagedViewSqlParseException_InnerExceptionConstructor()
    {
        var inner = new FormatException("format");
        var ex = new ManagedViewSqlParseException("parse error", inner);
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void ManagedViewProviderException_DefaultConstructor()
    {
        var ex = new ManagedViewProviderException();
        ex.Should().BeAssignableTo<ManagedViewException>();
    }

    [Fact]
    public void ManagedViewProviderException_MessageConstructor()
    {
        var ex = new ManagedViewProviderException("unsupported");
        ex.Message.Should().Be("unsupported");
    }

    [Fact]
    public void ManagedViewProviderException_InnerExceptionConstructor()
    {
        var inner = new NotSupportedException();
        var ex = new ManagedViewProviderException("msg", inner);
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void ManagedViewNotFoundException_DefaultConstructor()
    {
        var ex = new ManagedViewNotFoundException();
        ex.Should().BeAssignableTo<ManagedViewException>();
        ex.ViewName.Should().BeEmpty();
    }

    [Fact]
    public void ManagedViewNotFoundException_ViewNameConstructor_SetsViewName()
    {
        var ex = new ManagedViewNotFoundException("vw_missing");
        ex.ViewName.Should().Be("vw_missing");
        ex.Message.Should().Contain("vw_missing");
    }

    [Fact]
    public void ManagedViewNotFoundException_InnerExceptionConstructor()
    {
        var inner = new InvalidOperationException("cause");
        var ex = new ManagedViewNotFoundException("not found", inner);
        ex.InnerException.Should().BeSameAs(inner);
        ex.ViewName.Should().BeEmpty();
    }

    [Fact]
    public void ManagedViewDuplicateException_DefaultConstructor()
    {
        var ex = new ManagedViewDuplicateException();
        ex.Should().BeAssignableTo<ManagedViewException>();
        ex.ViewName.Should().BeEmpty();
        ex.Source1.Should().BeEmpty();
        ex.Source2.Should().BeEmpty();
    }

    [Fact]
    public void ManagedViewDuplicateException_MessageConstructor()
    {
        var ex = new ManagedViewDuplicateException("duplicate");
        ex.Message.Should().Be("duplicate");
        ex.ViewName.Should().BeEmpty();
        ex.Source1.Should().BeEmpty();
        ex.Source2.Should().BeEmpty();
    }

    [Fact]
    public void ManagedViewDuplicateException_InnerExceptionConstructor()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new ManagedViewDuplicateException("msg", inner);
        ex.InnerException.Should().BeSameAs(inner);
        ex.ViewName.Should().BeEmpty();
    }

    [Fact]
    public void ManagedViewDuplicateException_DetailedConstructor_SetsProperties()
    {
        var ex = new ManagedViewDuplicateException("vw_dup", "source_a", "source_b");
        ex.ViewName.Should().Be("vw_dup");
        ex.Source1.Should().Be("source_a");
        ex.Source2.Should().Be("source_b");
        ex.Message.Should().Contain("vw_dup");
        ex.Message.Should().Contain("source_a");
        ex.Message.Should().Contain("source_b");
    }

    [Fact]
    public void ManagedViewDependencyCycleException_DefaultConstructor()
    {
        var ex = new ManagedViewDependencyCycleException();
        ex.Should().BeAssignableTo<ManagedViewException>();
        ex.CycleNodes.Should().BeEmpty();
    }

    [Fact]
    public void ManagedViewDependencyCycleException_MessageConstructor()
    {
        var ex = new ManagedViewDependencyCycleException("cycle found");
        ex.Message.Should().Be("cycle found");
        ex.CycleNodes.Should().BeEmpty();
    }

    [Fact]
    public void ManagedViewDependencyCycleException_InnerExceptionConstructor()
    {
        var inner = new InvalidOperationException("root cause");
        var ex = new ManagedViewDependencyCycleException("cycle", inner);
        ex.InnerException.Should().BeSameAs(inner);
        ex.CycleNodes.Should().BeEmpty();
    }

    [Fact]
    public void ManagedViewDependencyCycleException_WithCycleNodes()
    {
        var nodes = new List<string> { "A", "B", "C" };
        var ex = new ManagedViewDependencyCycleException("cycle detected", nodes);
        ex.CycleNodes.Should().BeEquivalentTo(["A", "B", "C"]);
    }

    [Fact]
    public void ManagedViewDependencyCycleException_NullCycleNodes_DefaultsToEmpty()
    {
        var ex = new ManagedViewDependencyCycleException("cycle", (IReadOnlyList<string>?)null);
        ex.CycleNodes.Should().BeEmpty();
    }
}
