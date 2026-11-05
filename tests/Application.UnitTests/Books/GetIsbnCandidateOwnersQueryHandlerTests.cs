using System.Reflection;
using Application.Books.IsbnOwners;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.Books;

public sealed class GetIsbnCandidateOwnersQueryHandlerTests
{
    private const string UsedIsbn = "9782800112343";
    private const string FreeIsbn = "2205056174";

    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Guid s_libraryId = Guid.CreateVersion7();

    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly GetIsbnCandidateOwnersQueryHandler _handler;
    private readonly DigitalBook _book = DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", null), s_libraryId, "/data/A/b.cbz", 1).Value!;

    public GetIsbnCandidateOwnersQueryHandlerTests()
    {
        _handler = new GetIsbnCandidateOwnersQueryHandler(_bookRepository, _libraryRepository);
        _bookRepository.GetByIdAsync(_book.Id).Returns(_book);
        _libraryRepository.GetByIdAsync(s_libraryId).Returns(Library.Create("Imported", "#FF0000", "book", LibraryBookType.Digital, s_userId).Value!);
    }

    private static DigitalBook CreateOwner(string libraryName)
    {
        var owner = DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", UsedIsbn, 4, "https://covers/blacksad.jpg"), Guid.CreateVersion7(), "/data/B/b.cbz", 1).Value!;
        var library = Library.Create(libraryName, "#00FF00", "bd", LibraryBookType.Digital, s_userId).Value!;
        typeof(Book).GetProperty(nameof(Book.Library), BindingFlags.Public | BindingFlags.Instance)!.SetValue(owner, library);
        return owner;
    }

    private Task<Domain.Primitives.Result<IReadOnlyList<IsbnOwnerDto>>> HandleAsync(Guid? bookId = null, Guid? userId = null) =>
        _handler.Handle(new GetIsbnCandidateOwnersQuery(bookId ?? _book.Id, userId ?? s_userId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_ReturnTheBooksHoldingTheIsbnsReadOnThePages()
    {
        _book.RecordIsbnScan([UsedIsbn, FreeIsbn], DateTime.UtcNow);
        var owner = CreateOwner("BD");
        _bookRepository.GetByIsbnAsync(UsedIsbn, s_libraryId, Arg.Any<CancellationToken>()).Returns(owner);

        var result = await HandleAsync();

        result.Value.Should().Equal(new IsbnOwnerDto(UsedIsbn, owner.Id, "Blacksad", "Âme rouge", 4, "https://covers/blacksad.jpg", "BD"));
    }

    [Fact]
    public async Task Handle_Should_ReturnNoOwner_WhenTheBookIsNotDigital()
    {
        var physical = PhysicalBook.Create(new BookMetadata("Astérix", "Le Tour de Gaule", "978-2-01-210135-7"), s_libraryId).Value!;
        _bookRepository.GetByIdAsync(physical.Id).Returns(physical);

        var result = await HandleAsync(physical.Id);

        result.Value.Should().BeEmpty();
        await _bookRepository.DidNotReceive().GetByIsbnAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheBookIdIsEmpty()
    {
        var result = await HandleAsync(Guid.Empty);

        result.Error.Should().Be(BooksError.BadRequest);
        await _bookRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTheBookDoesNotExist()
    {
        var result = await HandleAsync(Guid.CreateVersion7());

        result.Error.Should().Be(BooksError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTheBookBelongsToAnotherUser()
    {
        var result = await HandleAsync(userId: Guid.CreateVersion7());

        result.Error.Should().Be(BooksError.NotFound);
        await _bookRepository.DidNotReceive().GetByIsbnAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
