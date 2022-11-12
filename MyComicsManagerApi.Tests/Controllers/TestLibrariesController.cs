using System.Net;
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
    public void GetAll_ShouldReturn200Status()
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
    }
    
    [Fact]
    public void GetId_ShouldReturn200Status()
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
    }
    
    [Fact]
    public void GetId_ShouldReturn404Status()
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
    }
    
    [Fact]
    public void Create_ShouldReturn201Status()
    {
        // Arrange
        var libService = new Mock<ILibraryService>();
        var mockLogger = new Mock<ILogger<LibrariesController>>();
        var lib = new Library
        {
            Id = "1",
            Name = "Lib1",
            RelPath = "relPath1"
        };

        var controller = new LibrariesController(mockLogger.Object, libService.Object);
 
        // Act
        var actionResult = controller.Create(lib);
        
        // Assert
        actionResult.Should().NotBeNull();
        actionResult.Result.Should().BeOfType<CreatedAtRouteResult>();
        var res = (CreatedAtRouteResult)actionResult.Result;
        res!.StatusCode.Should().Be((int)HttpStatusCode.Created);
        res.RouteName.Should().Be("GetLibrary");
    }
    
    [Fact]
    public void Create_ShouldReturn400Status()
    {
        // Arrange
        var libService = new Mock<ILibraryService>();
        var mockLogger = new Mock<ILogger<LibrariesController>>();

        var controller = new LibrariesController(mockLogger.Object, libService.Object);
 
        // Act
        var actionResult = controller.Create(null);
        
        // Assert
        actionResult.Should().NotBeNull();
        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
        var res = (BadRequestObjectResult)actionResult.Result;
        res!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }
}