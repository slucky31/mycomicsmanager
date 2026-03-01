using Application.Abstractions.Messaging;
using Domain.Libraries;
using NetArchTest.Rules;

namespace Architecture.Tests;

public class ArchitectureTests
{
    private const string ApplicationNamespace = "Application";
    private const string PersistenceNamespace = "Persistence";
    private const string WebNamespace = "Web";

    private static readonly System.Reflection.Assembly DomainAssembly =
        typeof(Library).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly =
        typeof(Application.ApplicationDependencyInjection).Assembly;
    private static readonly System.Reflection.Assembly PersistenceAssembly =
        typeof(Persistence.ProjectDependencyInjection).Assembly;

    // -------------------------------------------------------
    // Layer dependency rules
    // -------------------------------------------------------

    [Fact]
    public void Domain_Should_Not_HaveDependencyOnOtherProjects()
    {
        var testResult = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, PersistenceNamespace, WebNamespace)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_HaveDependencyOnOtherProjects()
    {
        var testResult = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(PersistenceNamespace, WebNamespace)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Persistence_Should_Not_HaveDependencyOnOtherProjects()
    {
        var testResult = Types.InAssembly(PersistenceAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(WebNamespace)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue();
    }

    // -------------------------------------------------------
    // CQRS naming conventions
    // -------------------------------------------------------

    [Fact]
    public void Commands_Should_HaveNameEndingWithCommand()
    {
        var testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreClasses()
            .And()
            .ImplementInterface(typeof(IBaseCommand))
            .Should()
            .HaveNameEndingWith("Command", StringComparison.Ordinal)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Queries_Should_HaveNameEndingWithQuery()
    {
        var testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Query", StringComparison.Ordinal)
            .Should()
            .ImplementInterface(typeof(IQuery<>))
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CommandHandlers_Should_HaveNameEndingWithCommandHandler()
    {
        var testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Should()
            .HaveNameEndingWith("CommandHandler", StringComparison.Ordinal)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void QueryHandlers_Should_HaveNameEndingWithQueryHandler()
    {
        var testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .HaveNameEndingWith("QueryHandler", StringComparison.Ordinal)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue();
    }

    // -------------------------------------------------------
    // CQRS sealed convention
    // -------------------------------------------------------

    [Fact]
    public void CommandHandlers_Should_BeSealed()
    {
        var testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("CommandHandler", StringComparison.Ordinal)
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void QueryHandlers_Should_BeSealed()
    {
        var testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("QueryHandler", StringComparison.Ordinal)
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue();
    }

    // -------------------------------------------------------
    // Application interface naming convention
    // -------------------------------------------------------

    [Fact]
    public void ApplicationInterfaces_Should_HaveNameStartingWithI()
    {
        var testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I", StringComparison.Ordinal)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue();
    }
}
