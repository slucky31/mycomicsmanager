using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyComicsManager.Model.Shared.Models;
using MyComicsManagerApi.Controllers;
using MyComicsManagerApi.Services;
using MyComicsManagerApiTests.MockData;
using Xunit;

namespace MyComicsManagerApiTests.Controllers;

public class TestLibrariesController
{
    private readonly LibrariesController _controller;
    private readonly ILibraryService _mockLibraryService;
    private readonly ILogger<LibrariesController> _mockLogger;
    
    public TestLibrariesController()
    {
        _mockLibraryService = Substitute.For<ILibraryService>();
        _mockLogger = Substitute.For<ILogger<LibrariesController>>();
        _controller = new LibrariesController(_mockLogger, _mockLibraryService);
    }
    
    [Fact]
    public void GetAll_ShouldReturn200Status()
    {
        // Arrange
        _mockLibraryService.Get().Returns(LibraryMockData.Get());

        // Act
        var actionResult = _controller.Get();
        
        // Assert
        actionResult.Value!.Count.Should().Be(4);
    }
    
    [Fact]
    public void GetId_ShouldReturn200Status()
    {
        // Arrange
        _mockLibraryService.Get("1").Returns(LibraryMockData.GetId("1"));
        var sut = new LibrariesController(_mockLogger, _mockLibraryService);
 
        // Act
        var actionResult = sut.Get("1");
        
        // Assert
        var lib = actionResult.Value;
        lib!.Id.Should().Be("1");
        lib!.RelPath.Should().Be("lib1");
        lib!.Name.Should().Be("Library 1");
    }
    
    [Fact]
    public void GetId_ShouldReturn404Status()
    {
        // Arrange
        _mockLibraryService.Get("1").Returns((Library) null);
        var sut = new LibrariesController(_mockLogger, _mockLibraryService);
 
        // Act
        var actionResult = sut.Get("1");
        
        // Assert
        actionResult.Result.Should().BeOfType<NotFoundResult>();
        var res = (NotFoundResult)actionResult.Result;
        res!.StatusCode.Should().Be(404);
    }
    
    [Fact]
    public void Create_ShouldReturn201Status()
    {
        // Arrange
        var lib = new Library
        {
            Id = "1",
            Name = "Lib1",
            RelPath = "relPath1"
        };
        var controller = new LibrariesController(_mockLogger, _mockLibraryService);
 
        // Act
        var actionResult = controller.Create(lib);
        
        // Assert
        actionResult.Result.Should().BeOfType<CreatedAtRouteResult>();
        var res = (CreatedAtRouteResult)actionResult.Result;
        res!.StatusCode.Should().Be((int)HttpStatusCode.Created);
        res.RouteName.Should().Be("GetLibrary");
    }
    
    [Fact]
    public void Create_ShouldReturn400Status()
    {
        // Arrange

        // Act
        var actionResult = _controller.Create(null);
        
        // Assert
        actionResult.Should().NotBeNull();
        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
        var res = (BadRequestObjectResult)actionResult.Result;
        res!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public void Update_ShouldReturn204Status()
    {
        // Arrange
        var lib = new Library
        {
            Id = "1",
            Name = "Lib1",
            RelPath = "relPath1"
        };
        _mockLibraryService.Get("1").Returns(lib);

        // Act
        var actionResult = _controller.Update(lib.Id, lib) as NoContentResult;
        
        // Assert
        actionResult.Should().NotBeNull();
        actionResult.Should().BeOfType<NoContentResult>();
        actionResult!.StatusCode.Should().Be((int) HttpStatusCode.NoContent);
    }
    
    [Fact]
    public void Update_ShouldReturn404Status()
    {
        // Arrange
        var lib = new Library
        {
            Id = "1",
            Name = "Lib1",
            RelPath = "relPath1"
        };
        _mockLibraryService.Get("1").Returns((Library) null);
        
        // Act
        var actionResult = _controller.Update(lib.Id, lib) as NotFoundResult;
        
        // Assert
        actionResult.Should().BeOfType<NotFoundResult>();
    }
    
    [Fact]
    public void ShouldReturnNotFound_WhenLibraryDoesNotExist()
    {
        // Arrange
        var id = "nonexistent-id";
        _mockLibraryService.Get(id).Returns((Library)null);

        // Act
        var response = _controller.Delete(id);

        // Assert
        response.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void ShouldReturnNoContent_WhenLibraryExists()
    {
        // Arrange
        var id = "existing-id";
        var library = new Library { Id = id };
        _mockLibraryService.Get(id).Returns(library);

        // Act
        var response = _controller.Delete(id);

        // Assert
        response.Should().BeOfType<NoContentResult>();        
    }
}