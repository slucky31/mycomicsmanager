using System.Reflection;
using Application.Books.IsbnAvailability;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.Books;

public sealed class IsIsbnAvailableQueryHandlerTests
{
    private const string Isbn = "978-2-01-210135-7";
    private const string NormalizedIsbn = "9782012101357";

    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Guid s_libraryId = Guid.CreateVersion7();

    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly IsIsbnAvailableQueryHandler _handler;
    private readonly PhysicalBook _book = CreateBook("Astérix et les Goths", "978-3-16-148410-0");

    public IsIsbnAvailableQueryHandlerTests()
    {
        _handler = new IsIsbnAvailableQueryHandler(_bookRepository, _libraryRepository);
        _bookRepository.GetByIdAsync(_book.Id).Returns(_book);
        _libraryRepository.GetByIdAsync(s_libraryId).Returns(Library.Create("Imported", "#FF0000", "book", LibraryBookType.Physical, s_userId).Value!);
    }

    private static PhysicalBook CreateBook(string title, string isbn)
    {
        var book = PhysicalBook.Create(new BookMetadata("Astérix", title, isbn, 3, string.Empty), s_libraryId).Value!;
        typeof(Book).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)?.SetValue(book, Guid.CreateVersion7());
        return book;
    }

    [Fact]
    public async Task Handle_Should_ReturnTrue_WhenNoOtherBookHoldsTheIsbn()
    {
        var result = await _handler.Handle(new IsIsbnAvailableQuery(_book.Id, Isbn, s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().BeTrue();
        await _bookRepository.Received(1).GetByIsbnAsync(NormalizedIsbn, s_libraryId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFalse_WhenAnotherBookOfTheSameTypeHoldsTheIsbn()
    {
        _bookRepository.GetByIsbnAsync(NormalizedIsbn, s_libraryId, Arg.Any<CancellationToken>()).Returns(CreateBook("Another", NormalizedIsbn));

        var result = await _handler.Handle(new IsIsbnAvailableQuery(_book.Id, Isbn, s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ReturnTrue_WhenTheBookHoldsTheIsbnItself()
    {
        _bookRepository.GetByIsbnAsync(NormalizedIsbn, s_libraryId, Arg.Any<CancellationToken>()).Returns(_book);

        var result = await _handler.Handle(new IsIsbnAvailableQuery(_book.Id, Isbn, s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("978-2-01-210135-2")]
    public async Task Handle_Should_ReturnInvalidIsbn_WhenTheIsbnIsNotValid(string isbn)
    {
        var result = await _handler.Handle(new IsIsbnAvailableQuery(_book.Id, isbn, s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.InvalidISBN);
        await _bookRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheBookIdIsEmpty()
    {
        var result = await _handler.Handle(new IsIsbnAvailableQuery(Guid.Empty, Isbn, s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTheBookDoesNotExist()
    {
        var result = await _handler.Handle(new IsIsbnAvailableQuery(Guid.CreateVersion7(), Isbn, s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTheBookBelongsToAnotherUser()
    {
        var result = await _handler.Handle(new IsIsbnAvailableQuery(_book.Id, Isbn, Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.NotFound);
        await _bookRepository.DidNotReceive().GetByIsbnAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
