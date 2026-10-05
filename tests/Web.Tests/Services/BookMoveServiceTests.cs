using Application.Abstractions.Messaging;
using Application.Books.Move;
using Application.Books.MoveTargets;
using Application.Interfaces;
using AwesomeAssertions;
using Domain.Books;
using Domain.Primitives;
using Domain.Users;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class BookMoveServiceTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IQueryHandler<GetBookMoveTargetsQuery, BookMoveTargets> _targetsHandler = Substitute.For<IQueryHandler<GetBookMoveTargetsQuery, BookMoveTargets>>();
    private readonly ICommandHandler<MoveBookCommand, Book> _moveHandler = Substitute.For<ICommandHandler<MoveBookCommand, Book>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly BookMoveService _service;

    public BookMoveServiceTests()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(s_userId);
        _service = new BookMoveService(_targetsHandler, _moveHandler, _currentUserService);
    }

    [Fact]
    public async Task GetTargetsAsync_Should_QueryForCurrentUser()
    {
        var bookId = Guid.CreateVersion7();
        var targets = new BookMoveTargets([], null);
        _targetsHandler.Handle(new GetBookMoveTargetsQuery(bookId, s_userId), Arg.Any<CancellationToken>()).Returns(targets);

        var result = await _service.GetTargetsAsync(bookId, TestContext.Current.CancellationToken);

        result.Value.Should().BeSameAs(targets);
    }

    [Fact]
    public async Task MoveAsync_Should_SendCommandForCurrentUser()
    {
        var bookId = Guid.CreateVersion7();
        var libraryId = Guid.CreateVersion7();
        _moveHandler.Handle(Arg.Any<MoveBookCommand>(), Arg.Any<CancellationToken>()).Returns(Result<Book>.Failure(BooksError.AlreadyInLibrary));

        var result = await _service.MoveAsync(bookId, libraryId, TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.AlreadyInLibrary);
        await _moveHandler.Received(1).Handle(new MoveBookCommand(bookId, libraryId, s_userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveAsync_And_GetTargetsAsync_Should_ReturnError_WhenUserIsUnknown()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(Result<Guid>.Failure(UsersError.NotFound));

        (await _service.GetTargetsAsync(Guid.CreateVersion7(), TestContext.Current.CancellationToken)).Error.Should().Be(UsersError.NotFound);
        (await _service.MoveAsync(Guid.CreateVersion7(), Guid.CreateVersion7(), TestContext.Current.CancellationToken)).Error.Should().Be(UsersError.NotFound);
        await _moveHandler.DidNotReceive().Handle(Arg.Any<MoveBookCommand>(), Arg.Any<CancellationToken>());
    }
}
