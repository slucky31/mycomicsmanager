using Application.Abstractions.Messaging;
using Application.Books.Delete;
using Domain.Books;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Books;

public class DeleteBooksCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly ICommandHandler<DeleteBookCommand> _deleteBook = Substitute.For<ICommandHandler<DeleteBookCommand>>();
    private readonly DeleteBooksCommandHandler _handler;

    public DeleteBooksCommandHandlerTests()
    {
        _deleteBook.Handle(Arg.Any<DeleteBookCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new DeleteBooksCommandHandler(_deleteBook, NullLogger<DeleteBooksCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_DeleteEveryBookOnce_AndCountThoseThatStayed()
    {
        var deleted = Guid.CreateVersion7();
        var stayed = Guid.CreateVersion7();
        var alsoStayed = Guid.CreateVersion7();
        _deleteBook.Handle(new DeleteBookCommand(stayed, s_userId), Arg.Any<CancellationToken>()).Returns(Result.Failure(BooksError.NotFound));
        _deleteBook.Handle(new DeleteBookCommand(alsoStayed, s_userId), Arg.Any<CancellationToken>()).Returns(Result.Failure(BooksError.BadRequest));

        var result = await _handler.Handle(new DeleteBooksCommand([deleted, stayed, deleted, alsoStayed], s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().Be(new DeleteBooksResult(1, 2, BooksError.NotFound));
        await _deleteBook.Received(1).Handle(new DeleteBookCommand(deleted, s_userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheBooksOrTheUserAreNotGiven()
    {
        var tooMany = Enumerable.Range(0, DeleteBooksCommandHandler.MaxBooks + 1).Select(_ => Guid.CreateVersion7()).ToList();
        var book = Guid.CreateVersion7();

        foreach (var command in new[]
        {
            new DeleteBooksCommand([], s_userId),
            new DeleteBooksCommand(tooMany, s_userId),
            new DeleteBooksCommand([book, Guid.Empty], s_userId),
            new DeleteBooksCommand([book], Guid.Empty),
        })
        {
            (await _handler.Handle(command, TestContext.Current.CancellationToken)).Error.Should().Be(BooksError.BadRequest);
        }

        await _deleteBook.DidNotReceive().Handle(Arg.Any<DeleteBookCommand>(), Arg.Any<CancellationToken>());
    }
}
