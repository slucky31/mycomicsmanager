using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
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
        actionResult.Value.Count.Should().Be(4);
        return Task.CompletedTask;
    }
}