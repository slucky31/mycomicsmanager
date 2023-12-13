﻿using Application.Data;
using Application.Librairies.Create;
using Ardalis.GuardClauses;
using Domain.Libraries;
using Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReceivedExtensions;

namespace Application.UnitTests.Libraries;
public class CreateLibraryCommandTests
{
    private static CreateLibraryCommand Command = new("test");

    private readonly CreateLibraryCommandHandler _handler;
    private readonly IRepository<Library, string> _librayRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;

    public CreateLibraryCommandTests()
    {
        _librayRepositoryMock = Substitute.For<IRepository<Library, string>>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();

        _handler = new CreateLibraryCommandHandler(_librayRepositoryMock, _unitOfWorkMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess()
    {
        // Arrange
        _librayRepositoryMock.Add(Arg.Any<Library>());

        // Act
        var result = await _handler.Handle(Command, default);
        Guard.Against.Null(result.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(Command.Name);
        _librayRepositoryMock.Received(1).Add(Arg.Any<Library>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Should_ExecuteSaveChangeAsyncOnce()
    {
        // Arrange
        _librayRepositoryMock.Add(Arg.Any<Library>());

        // Act
        await _handler.Handle(Command, default);

        // Assert
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

}