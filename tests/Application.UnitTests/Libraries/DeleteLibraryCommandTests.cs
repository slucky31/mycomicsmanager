﻿using Application.Interfaces;
using Application.Libraries;
using Application.Libraries.Delete;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Libraries;
public class DeleteLibraryCommandTests
{
    private static readonly DeleteLibraryCommand Command = new(Guid.CreateVersion7());
    private static readonly Library library = Library.Create("test");

    private readonly DeleteLibraryCommandHandler _handler;
    private readonly IRepository<Library, Guid> _librayRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ILibraryLocalStorage _libraryLocalStorageMock;

    public DeleteLibraryCommandTests()
    {
        _librayRepositoryMock = Substitute.For<IRepository<Library, Guid>>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _libraryLocalStorageMock = Substitute.For<ILibraryLocalStorage>();

        _handler = new DeleteLibraryCommandHandler(_librayRepositoryMock, _unitOfWorkMock, _libraryLocalStorageMock);
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
        _libraryLocalStorageMock.Delete(library.RelativePath).Returns(Result.Success());

        // Act
        var result = await _handler.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _librayRepositoryMock.Received(1).Remove(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);

    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenDirectoryIsNotDeleted()
    {
        // Arrange
        _librayRepositoryMock.GetByIdAsync(Command.Id).Returns(library);
        _libraryLocalStorageMock.Delete(library.RelativePath).Returns(Result.Failure(TError.Any));

        // Act
        var result = await _handler.Handle(Command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.FolderNotDeleted);
    }

}
