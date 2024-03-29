﻿using Application.Data;
using Domain.Libraries;
using FluentAssertions;
using NSubstitute;
using Application.Interfaces;
using MongoDB.Bson;
using Application.Libraries.Delete;

namespace Application.UnitTests.Libraries;
public class DeleteLibraryCommandTests
{
    private static readonly DeleteLibraryCommand Command = new(new ObjectId());
    private static readonly Library library = Library.Create("test");

    private readonly DeleteLibraryCommandHandler _handler;
    private readonly IRepository<Library, ObjectId> _librayRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;

    public DeleteLibraryCommandTests()
    {
        _librayRepositoryMock = Substitute.For<IRepository<Library, ObjectId>>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();

        _handler = new DeleteLibraryCommandHandler(_librayRepositoryMock, _unitOfWorkMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenLibraryIsNull()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(Command.Id).Returns((Library?)null);

        // Act
        var result = await _handler.Handle(Command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
        _librayRepositoryMock.Received(0).Remove(Arg.Any<Library>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(Command.Id).Returns(library);

        // Act
        var result = await _handler.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _librayRepositoryMock.Received(1).Remove(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
        
    }

}
