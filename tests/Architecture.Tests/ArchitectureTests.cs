using Domain.Libraries;
using FluentAssertions;
using NetArchTest.Rules;

namespace Architecture.Tests;

// https://www.youtube.com/watch?v=_D6Kai4RdGY
public class ArchitectureTests
{
    private const string ApplicationNamespace = "Application";
    private const string PersistenceNamespace = "Persistence";
    private const string PresentationNamespace = "Presentation";
    private const string WebApiNamespace = "WebAPI";

    [Fact]
    public void Domain_Should_Not_HaveDependencyOnOtherProjects()
    {
        // Arrange
        var assembly = typeof(Library).Assembly;

        var otherProjects = new[]
        {
            ApplicationNamespace,
            PersistenceNamespace,
            PresentationNamespace,
            WebApiNamespace,
        };

        // Act
        var testResult = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAll(otherProjects)
            .GetResult();

        // Assert
        testResult.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_HaveDependencyOnOtherProjects()
    {
        // Arrange
        var assembly = typeof(Application.ApplicationDependencyInjection).Assembly;

        var otherProjects = new[]
        {
            PersistenceNamespace,
            PresentationNamespace,
            WebApiNamespace,
        };

        // Act
        var testResult = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAll(otherProjects)
            .GetResult();

        // Assert
        testResult.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Persistence_Should_Not_HaveDependencyOnOtherProjects()
    {
        // Arrange
        var assembly = typeof(Persistence.ProjectDependencyInjection).Assembly;

        var otherProjects = new[]
        {
            PresentationNamespace,
            WebApiNamespace,
        };

        // Act
        var testResult = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAll(otherProjects)
            .GetResult();

        // Assert
        testResult.IsSuccessful.Should().BeTrue();
    }
}
