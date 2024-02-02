using Application.Data;
using Application.Libraries.Create;
using Ardalis.GuardClauses;
using Domain.Libraries;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Application.Interfaces;
using MongoDB.Bson;
using NSubstitute.ReturnsExtensions;
using Application.Libraries.GetById;

namespace Application.UnitTests.Libraries;
public class GetLibraryCommandTests
{
 
    private readonly GetLibraryQueryHandler _handler;
    private readonly IRepository<Library, ObjectId> _librayRepositoryMock;
    private readonly ObjectId libraryId = ObjectId.GenerateNewId();


    public GetLibraryCommandTests()
    {
        _librayRepositoryMock = Substitute.For<IRepository<Library, ObjectId>>();
        _handler = new GetLibraryQueryHandler(_librayRepositoryMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess()
    {
        // Arrange
        var library = Library.Create("test", libraryId);
        _librayRepositoryMock.GetByIdAsync(libraryId).Returns(library);
        var Query = new GetLibraryQuery(libraryId);       

        // Act
        var result = await _handler.Handle(Query, default);
        Guard.Against.Null(result.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(libraryId);        
        result.Value.Name.Should().Be(library.Name);
        await _librayRepositoryMock.Received(1).GetByIdAsync(Arg.Any<ObjectId>());        
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenLibraryIsNotFound()
    {
        // Arrange        
        _librayRepositoryMock.GetByIdAsync(libraryId).ReturnsNull();
        var Query = new GetLibraryQuery(libraryId);

        // Act
        var result = await _handler.Handle(Query, default);        

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesErrors.NotFound);
        await _librayRepositoryMock.Received(1).GetByIdAsync(Arg.Any<ObjectId>());
    }



}
