using Application.Abstractions.Messaging;
using Application.Books.Move;
using Application.Books.MoveTargets;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Books;

public class MoveBooksCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Guid s_targetId = Guid.CreateVersion7();

    private readonly ICommandHandler<MoveBookCommand, Book> _moveBook = Substitute.For<ICommandHandler<MoveBookCommand, Book>>();
    private readonly MoveBooksCommandHandler _handler;

    public MoveBooksCommandHandlerTests()
    {
        _moveBook.Handle(Arg.Any<MoveBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => Result<Book>.Success(PhysicalBook.Create(new BookMetadata("Blacksad", "Âme rouge", "9782205050134"), s_targetId).Value!));
        _handler = new MoveBooksCommandHandler(_moveBook, NullLogger<MoveBooksCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_MoveEveryBookOnce_AndCountThoseThatStayed()
    {
        var moved = Guid.CreateVersion7();
        var stayed = Guid.CreateVersion7();
        var alsoStayed = Guid.CreateVersion7();
        _moveBook.Handle(new MoveBookCommand(stayed, s_targetId, s_userId), Arg.Any<CancellationToken>()).Returns(Result<Book>.Failure(BooksError.FileAlreadyExists));
        _moveBook.Handle(new MoveBookCommand(alsoStayed, s_targetId, s_userId), Arg.Any<CancellationToken>()).Returns(Result<Book>.Failure(BooksError.FileNotFound));

        var result = await _handler.Handle(new MoveBooksCommand([moved, stayed, moved, alsoStayed], s_targetId, s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().Be(new MoveBooksResult(1, 2, BooksError.FileAlreadyExists));
        await _moveBook.Received(1).Handle(new MoveBookCommand(moved, s_targetId, s_userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheBooksTheLibraryOrTheUserAreNotGiven()
    {
        var tooMany = Enumerable.Range(0, GetBookMoveTargetsQueryHandler.MaxBooks + 1).Select(_ => Guid.CreateVersion7()).ToList();
        var book = Guid.CreateVersion7();

        foreach (var command in new[]
        {
            new MoveBooksCommand([], s_targetId, s_userId),
            new MoveBooksCommand(tooMany, s_targetId, s_userId),
            new MoveBooksCommand([book, Guid.Empty], s_targetId, s_userId),
            new MoveBooksCommand([book], Guid.Empty, s_userId),
            new MoveBooksCommand([book], s_targetId, Guid.Empty),
        })
        {
            (await _handler.Handle(command, TestContext.Current.CancellationToken)).Error.Should().Be(BooksError.BadRequest);
        }

        await _moveBook.DidNotReceive().Handle(Arg.Any<MoveBookCommand>(), Arg.Any<CancellationToken>());
    }
}
