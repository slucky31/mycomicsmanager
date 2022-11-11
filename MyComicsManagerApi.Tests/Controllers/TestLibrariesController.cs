using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyComicsManager.Model.Shared.Models;
using MyComicsManagerApi.Controllers;
using MyComicsManagerApi.Services;
using MyComicsManagerApiTests.MockData;
using Xunit;

namespace MyComicsManagerApiTests.Controllers;

public class TestLibrariesController
{
    [Fact]
    public Task GetAll_ShouldReturn200Status()
    {
        // Arrange
        var libService = new Mock<ILibraryService>();
        var mockLogger = new Mock<ILogger<LibrariesController>>();
        libService.Setup(_ => _.Get()).Returns(LibraryMockData.Get());
        var sut = new LibrariesController(mockLogger.Object, libService.Object);
 
        // Act
        var actionResult = sut.Get();
        
        // Assert
        actionResult.Should().NotBeNull();
        actionResult.Value.Should().NotBeNull();
        actionResult.Value!.Count.Should().Be(4);
        return Task.CompletedTask;
    }
    
    [Fact]
    public Task GetId_ShouldReturn200Status()
    {
        // Arrange
        var libService = new Mock<ILibraryService>();
        var mockLogger = new Mock<ILogger<LibrariesController>>();
        libService.Setup(_ => _.Get("1")).Returns(LibraryMockData.GetId("1"));
        var sut = new LibrariesController(mockLogger.Object, libService.Object);
 
        // Act
        var actionResult = sut.Get("1");
        
        // Assert
        actionResult.Should().NotBeNull();
        actionResult.Value.Should().NotBeNull();
        var lib = actionResult.Value;
        lib!.Id.Should().Be("1");
        lib!.RelPath.Should().Be("lib1");
        lib!.Name.Should().Be("Library 1");
        return Task.CompletedTask;
    }
    
    [Fact]
    public Task GetId_ShouldReturn404Status()
    {
        // Arrange
        var libService = new Mock<ILibraryService>();
        var mockLogger = new Mock<ILogger<LibrariesController>>();
        libService.Setup(_ => _.Get("1")).Returns((Library) null);
        var sut = new LibrariesController(mockLogger.Object, libService.Object);
 
        // Act
        var actionResult = sut.Get("1");
        
        // Assert
        actionResult.Should().NotBeNull();
        actionResult.Result.Should().NotBeNull();
        actionResult.Result.Should().BeOfType<NotFoundResult>();
        var res = (NotFoundResult)actionResult.Result;
        res!.StatusCode.Should().Be(404);
        return Task.CompletedTask;
    }
}