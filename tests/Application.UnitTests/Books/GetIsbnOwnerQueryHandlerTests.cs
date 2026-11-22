using System.Reflection;
using Application.Books.IsbnOwners;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Books;

public sealed class GetIsbnOwnerQueryHandlerTests
{
    private const string Isbn = "9782800112343";

    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Guid s_libraryId = Guid.CreateVersion7();

    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly GetIsbnOwnerQueryHandler _handler;
    private readonly PhysicalBook _owner;

    public GetIsbnOwnerQueryHandlerTests()
    {
        _handler = new GetIsbnOwnerQueryHandler(_bookRepository, _libraryRepository);
        _libraryRepository.GetByIdAsync(s_libraryId).Returns(Library.Create("BD", "#FF0000", "book", LibraryBookType.Physical, s_userId).Value!);

        _owner = PhysicalBook.Create(new BookMetadata("Blacksad", "Âme rouge", Isbn, 4, "https://covers/blacksad.jpg"), Guid.CreateVersion7()).Value!;
        var ownerLibrary = Library.Create("Mangas", "#00FF00", "bd", LibraryBookType.Physical, s_userId).Value!;
        typeof(Book).GetProperty(nameof(Book.Library), BindingFlags.Public | BindingFlags.Instance)!.SetValue(_owner, ownerLibrary);
        _bookRepository.GetByIsbnAsync(Isbn, s_libraryId, Arg.Any<CancellationToken>()).Returns(_owner);
    }

    private Task<Result<IReadOnlyList<IsbnOwnerDto>>> HandleAsync(string isbn = Isbn, Guid? bookId = null, Guid? libraryId = null, Guid? userId = null) =>
        _handler.Handle(new GetIsbnOwnerQuery(libraryId ?? s_libraryId, bookId ?? Guid.Empty, isbn, userId ?? s_userId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_ReturnTheBookHoldingTheIsbn_WhenItIsTyped()
    {
        var result = await HandleAsync("978-2-8001-1234-3");

        result.Value.Should().Equal(new IsbnOwnerDto(Isbn, _owner.Id, "Blacksad", "Âme rouge", 4, "https://covers/blacksad.jpg", "Mangas"));
    }

    [Fact]
    public async Task Handle_Should_ReturnNoOwner_WhenTheEditedBookHoldsTheIsbn()
    {
        var result = await HandleAsync(bookId: _owner.Id);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_ReturnNoOwner_WhenNoBookHoldsTheIsbn()
    {
        var result = await HandleAsync("2205056174");

        result.Value.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    public async Task Handle_Should_ReturnNoOwnerWithoutLookingUp_WhenTheIsbnIsNotValid(string isbn)
    {
        var result = await HandleAsync(isbn);

        result.Value.Should().BeEmpty();
        await _libraryRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
        await _bookRepository.DidNotReceive().GetByIsbnAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheLibraryIdIsEmpty()
    {
        var result = await HandleAsync(libraryId: Guid.Empty);

        result.Error.Should().Be(BooksError.BadRequest);
        await _libraryRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheUserIdIsEmpty()
    {
        var result = await HandleAsync(userId: Guid.Empty);

        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheQueryIsNull()
    {
        var result = await _handler.Handle(null!, TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTheLibraryBelongsToAnotherUser()
    {
        var result = await HandleAsync(userId: Guid.CreateVersion7());

        result.Error.Should().Be(LibrariesError.NotFound);
        await _bookRepository.DidNotReceive().GetByIsbnAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTheLibraryDoesNotExist()
    {
        var result = await HandleAsync(libraryId: Guid.CreateVersion7());

        result.Error.Should().Be(LibrariesError.NotFound);
    }
}
