using Application.Data;
using Application.Librairies.Create;
using Ardalis.GuardClauses;
using Domain.Libraries;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Application.Interfaces;
using Application.Librairies.Get;
using Domain.Dto;
using MongoDB.Bson;
using NSubstitute.ReturnsExtensions;

namespace Application.UnitTests.Libraries;
public class GetLibraryCommandTests
{
 
    private readonly GetLibraryQueryHandler _handler;
    private readonly IRepository<LibraryDto, LibraryId> _librayRepositoryMock;
    private readonly LibraryId libraryId = new LibraryId(new ObjectId());


    public GetLibraryCommandTests()
    {
        _librayRepositoryMock = Substitute.For<IRepository<LibraryDto, LibraryId>>();
        _handler = new GetLibraryQueryHandler(_librayRepositoryMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess()
    {
        // Arrange
        var libraryDto = LibraryDto.Create(Library.Create("test", libraryId));
        _librayRepositoryMock.GetByIdAsync(libraryId).Returns(libraryDto);
        var Query = new GetLibraryQuery(libraryId);       

        // Act
        var result = await _handler.Handle(Query, default);
        Guard.Against.Null(result.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(libraryId);        
        result.Value.Name.Should().Be(libraryDto.Name);
        await _librayRepositoryMock.Received(1).GetByIdAsync(Arg.Any<LibraryId>());        
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
        await _librayRepositoryMock.Received(1).GetByIdAsync(Arg.Any<LibraryId>());
    }



}
