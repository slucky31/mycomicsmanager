using Application.Books.List;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using NSubstitute;

namespace Application.UnitTests.Books;

public class GetBooksQueryHandlerTests
{
    private static readonly GetBooksQuery s_query = new();

    private readonly GetBooksQueryHandler _handler;
    private readonly IBookRepository _bookRepositoryMock;

    public GetBooksQueryHandlerTests()
    {
        _bookRepositoryMock = Substitute.For<IBookRepository>();
        _handler = new GetBooksQueryHandler(_bookRepositoryMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenBooksExist()
    {
        // Arrange
        var book1 = Book.Create("Serie 1", "Title 1", "978-3-16-148410-0");
        var book2 = Book.Create("Serie 2", "Title 2", "978-0-306-40615-7");
        var book3 = Book.Create("Serie 3", "Title 3", "978-0-451-52493-5");
        var books = new List<Book> { book1, book2, book3 };

        _bookRepositoryMock.List().Returns(books);

        // Act
        var result = await _handler.Handle(s_query, CancellationToken.None);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain(book1);
        result.Value.Should().Contain(book2);
        result.Value.Should().Contain(book3);
        await _bookRepositoryMock.Received(1).List();
    }

    [Fact]
    public async Task Handle_Should_ReturnEmptyList_WhenNoBooksExist()
    {
        // Arrange
        var emptyBooks = new List<Book>();
        _bookRepositoryMock.List().Returns(emptyBooks);

        // Act
        var result = await _handler.Handle(s_query, CancellationToken.None);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _bookRepositoryMock.Received(1).List();
    }

    [Fact]
    public async Task Handle_Should_CallListAsyncOnce()
    {
        // Arrange
        var books = new List<Book>();
        _bookRepositoryMock.List().Returns(books);

        // Act
        await _handler.Handle(s_query, CancellationToken.None);

        // Assert
        await _bookRepositoryMock.Received(1).List();
    }

    [Fact]
    public async Task Handle_Should_ReturnAllBooksWithCorrectProperties()
    {
        // Arrange
        var book1 = Book.Create("Harry Potter", "Philosopher's Stone", "978-3-16-148410-0", 1, "https://example.com/hp1.jpg");
        var book2 = Book.Create("Lord of the Rings", "Fellowship of the Ring", "978-0-306-40615-7", 1, "https://example.com/lotr1.jpg");
        var books = new List<Book> { book1, book2 };

        _bookRepositoryMock.List().Returns(books);

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
        var singleBook = Book.Create("Test Serie", "Test Title", "978-3-16-148410-0");
        var books = new List<Book> { singleBook };

        _bookRepositoryMock.List().Returns(books);

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
            books.Add(Book.Create($"Serie {i}", $"Title {i}", $"978-3-16-14841{i:D}-0"));
        }

        _bookRepositoryMock.List().Returns(books);

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
        var book1 = Book.Create("Serie A", "Title A", "978-3-16-148410-0");
        var book2 = Book.Create("Serie B", "Title B", "978-0-306-40615-7");
        var book3 = Book.Create("Serie C", "Title C", "978-0-451-52493-5");
        var books = new List<Book> { book1, book2, book3 };

        _bookRepositoryMock.List().Returns(books);

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
        var book1 = Book.Create("Same Serie", "Volume 1", "978-3-16-148410-0", 1);
        var book2 = Book.Create("Same Serie", "Volume 2", "978-0-306-40615-7", 2);
        var book3 = Book.Create("Same Serie", "Volume 3", "978-0-451-52493-5", 3);
        var books = new List<Book> { book1, book2, book3 };

        _bookRepositoryMock.List().Returns(books);

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
            Book.Create("Serie 1", "Title 1", "978-3-16-148410-0"),
            Book.Create("Serie 2", "Title 2", "978-0-306-40615-7")
        };

        _bookRepositoryMock.List().Returns(books);

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
        var book1 = Book.Create("Serie 1", "Title 1", "978-3-16-148410-0", 1, "https://example.com/image1.jpg");
        var book2 = Book.Create("Serie 2", "Title 2", "978-0-306-40615-7", 1, "");
        var book3 = Book.Create("Serie 3", "Title 3", "978-0-451-52493-5");
        var books = new List<Book> { book1, book2, book3 };

        _bookRepositoryMock.List().Returns(books);

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
        var books = new List<Book> { Book.Create("Serie", "Title", "978-3-16-148410-0") };
        _bookRepositoryMock.List().Returns(books);

        // Act
        var result = await _handler.Handle(s_query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
    }
}
