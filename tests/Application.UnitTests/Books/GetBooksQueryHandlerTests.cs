using Application.Books.List;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.Books;

public class GetBooksQueryHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly GetBooksQuery s_query = new(s_userId);

    private readonly GetBooksQueryHandler _handler;
    private readonly IBookRepository _bookRepositoryMock;
    private readonly IRepository<Library, Guid> _libraryRepositoryMock;

    public GetBooksQueryHandlerTests()
    {
        _bookRepositoryMock = Substitute.For<IBookRepository>();
        _libraryRepositoryMock = Substitute.For<IRepository<Library, Guid>>();
        _handler = new GetBooksQueryHandler(_bookRepositoryMock, _libraryRepositoryMock);
    }

    private static PhysicalBook CreateBook(string serie, string title, string isbn, int volumeNumber = 1, string imageLink = "")
        => PhysicalBook.Create(serie, title, isbn, volumeNumber, imageLink, libraryId: Guid.CreateVersion7()).Value!;

    private static Library CreateLibrary(Guid userId)
        => Library.Create("Test", "#FF0000", "book", LibraryBookType.Physical, userId).Value!;

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenBooksExist()
    {
        // Arrange
        var book1 = CreateBook("Serie 1", "Title 1", "978-3-16-148410-0");
        var book2 = CreateBook("Serie 2", "Title 2", "978-0-306-40615-7");
        var book3 = CreateBook("Serie 3", "Title 3", "978-0-451-52493-5");
        var books = new List<Book> { book1, book2, book3 };

        _bookRepositoryMock.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns(books);

        // Act
        var result = await _handler.Handle(s_query, CancellationToken.None);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain(book1);
        result.Value.Should().Contain(book2);
        result.Value.Should().Contain(book3);
        await _bookRepositoryMock.Received(1).ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnEmptyList_WhenNoBooksExist()
    {
        // Arrange
        var emptyBooks = new List<Book>();
        _bookRepositoryMock.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns(emptyBooks);

        // Act
        var result = await _handler.Handle(s_query, CancellationToken.None);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _bookRepositoryMock.Received(1).ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CallListAsyncOnce()
    {
        // Arrange
        var books = new List<Book>();
        _bookRepositoryMock.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns(books);

        // Act
        await _handler.Handle(s_query, CancellationToken.None);

        // Assert
        await _bookRepositoryMock.Received(1).ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnAllBooksWithCorrectProperties()
    {
        // Arrange
        var book1 = CreateBook("Harry Potter", "Philosopher's Stone", "978-3-16-148410-0", 1, "https://example.com/hp1.jpg");
        var book2 = CreateBook("Lord of the Rings", "Fellowship of the Ring", "978-0-306-40615-7", 1, "https://example.com/lotr1.jpg");
        var books = new List<Book> { book1, book2 };

        _bookRepositoryMock.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns(books);

        // Act
        var result = await _handler.Handle(s_query, CancellationToken.None);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var firstBook = result.Value[0];
        firstBook.Serie.Should().Be("Harry Potter");
        firstBook.Title.Should().Be("Philosopher's Stone");
        firstBook.ISBN.Should().Be("978-3-16-148410-0");
        firstBook.VolumeNumber.Should().Be(1);
        firstBook.ImageLink.Should().Be("https://example.com/hp1.jpg");

        var secondBook = result.Value[1];
        secondBook.Serie.Should().Be("Lord of the Rings");
        secondBook.Title.Should().Be("Fellowship of the Ring");
        secondBook.ISBN.Should().Be("978-0-306-40615-7");
        secondBook.VolumeNumber.Should().Be(1);
        secondBook.ImageLink.Should().Be("https://example.com/lotr1.jpg");
    }

    [Fact]
    public async Task Handle_Should_ReturnSingleBook_WhenOnlyOneBookExists()
    {
        // Arrange
        var singleBook = CreateBook("Test Serie", "Test Title", "978-3-16-148410-0");
        var books = new List<Book> { singleBook };

        _bookRepositoryMock.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns(books);

        // Act
        var result = await _handler.Handle(s_query, CancellationToken.None);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Should().Be(singleBook);
    }

    [Fact]
    public async Task Handle_Should_ReturnManyBooks_WhenMultipleBooksExist()
    {
        // Arrange
        var books = new List<Book>();
        for (var i = 1; i <= 10; i++)
        {
            books.Add(CreateBook($"Serie {i}", $"Title {i}", $"978-3-16-14841{i:D}-0"));
        }

        _bookRepositoryMock.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns(books);

        // Act
        var result = await _handler.Handle(s_query, CancellationToken.None);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(10);
    }

    [Fact]
    public async Task Handle_Should_ReturnBooksInCorrectOrder()
    {
        // Arrange
        var book1 = CreateBook("Serie A", "Title A", "978-3-16-148410-0");
        var book2 = CreateBook("Serie B", "Title B", "978-0-306-40615-7");
        var book3 = CreateBook("Serie C", "Title C", "978-0-451-52493-5");
        var books = new List<Book> { book1, book2, book3 };

        _bookRepositoryMock.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns(books);

        // Act
        var result = await _handler.Handle(s_query, CancellationToken.None);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value[0].Should().Be(book1);
        result.Value[1].Should().Be(book2);
        result.Value[2].Should().Be(book3);
    }

    [Fact]
    public async Task Handle_Should_ReturnListWithDifferentVolumeNumbers()
    {
        // Arrange
        var book1 = CreateBook("Same Serie", "Volume 1", "978-3-16-148410-0", 1);
        var book2 = CreateBook("Same Serie", "Volume 2", "978-0-306-40615-7", 2);
        var book3 = CreateBook("Same Serie", "Volume 3", "978-0-451-52493-5", 3);
        var books = new List<Book> { book1, book2, book3 };

        _bookRepositoryMock.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns(books);

        // Act
        var result = await _handler.Handle(s_query, CancellationToken.None);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain(b => b.VolumeNumber == 1);
        result.Value.Should().Contain(b => b.VolumeNumber == 2);
        result.Value.Should().Contain(b => b.VolumeNumber == 3);
    }

    [Fact]
    public async Task Handle_Should_ReturnSameListReference()
    {
        // Arrange
        var books = new List<Book>
        {
            CreateBook("Serie 1", "Title 1", "978-3-16-148410-0"),
            CreateBook("Serie 2", "Title 2", "978-0-306-40615-7")
        };

        _bookRepositoryMock.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns(books);

        // Act
        var result = await _handler.Handle(s_query, CancellationToken.None);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(books);
    }

    [Fact]
    public async Task Handle_Should_ReturnBooksWithDefaultAndCustomImageLinks()
    {
        // Arrange
        var book1 = CreateBook("Serie 1", "Title 1", "978-3-16-148410-0", 1, "https://example.com/image1.jpg");
        var book2 = CreateBook("Serie 2", "Title 2", "978-0-306-40615-7", 1, "");
        var book3 = CreateBook("Serie 3", "Title 3", "978-0-451-52493-5");
        var books = new List<Book> { book1, book2, book3 };

        _bookRepositoryMock.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns(books);

        // Act
        var result = await _handler.Handle(s_query, CancellationToken.None);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value[0].ImageLink.Should().Be("https://example.com/image1.jpg");
        result.Value[1].ImageLink.Should().Be(string.Empty);
        result.Value[2].ImageLink.Should().Be(string.Empty);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessResult()
    {
        // Arrange
        var books = new List<Book> { CreateBook("Serie", "Title", "978-3-16-148410-0") };
        _bookRepositoryMock.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns(books);

        // Act
        var result = await _handler.Handle(s_query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenLibraryDoesNotExist()
    {
        // Arrange
        var libraryId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var query = new GetBooksQuery(LibraryId: libraryId, UserId: userId);

        _libraryRepositoryMock.GetByIdAsync(libraryId).Returns((Library?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
        await _bookRepositoryMock.DidNotReceive().ListByLibraryIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenLibraryBelongsToOtherUser()
    {
        // Arrange
        var libraryId = Guid.CreateVersion7();
        var ownerId = Guid.CreateVersion7();
        var requestingUserId = Guid.CreateVersion7();
        var query = new GetBooksQuery(LibraryId: libraryId, UserId: requestingUserId);

        var library = CreateLibrary(ownerId);
        _libraryRepositoryMock.GetByIdAsync(libraryId).Returns(library);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
        await _bookRepositoryMock.DidNotReceive().ListByLibraryIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBooks_WhenLibraryOwnershipVerified()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var libraryId = Guid.CreateVersion7();
        var query = new GetBooksQuery(LibraryId: libraryId, UserId: userId);

        var library = CreateLibrary(userId);
        var books = new List<Book> { CreateBook("Serie 1", "Title 1", "978-3-16-148410-0") };
        _libraryRepositoryMock.GetByIdAsync(libraryId).Returns(library);
        _bookRepositoryMock.ListByLibraryIdAsync(libraryId, Arg.Any<CancellationToken>()).Returns(books);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        await _bookRepositoryMock.Received(1).ListByLibraryIdAsync(libraryId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBooks_WhenLibraryExistsForUser()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var libraryId = Guid.CreateVersion7();
        var query = new GetBooksQuery(userId, LibraryId: libraryId);

        var library = CreateLibrary(userId);
        var books = new List<Book> { CreateBook("Serie 1", "Title 1", "978-3-16-148410-0") };
        _libraryRepositoryMock.GetByIdAsync(libraryId).Returns(library);
        _bookRepositoryMock.ListByLibraryIdAsync(libraryId, Arg.Any<CancellationToken>()).Returns(books);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }
}
