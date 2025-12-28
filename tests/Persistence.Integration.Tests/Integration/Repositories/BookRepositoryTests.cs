using Ardalis.GuardClauses;
using Base.Integration.Tests;
using Domain.Books;

namespace Persistence.Tests.Integration.Repositories;

[Collection("DatabaseCollectionTests")]
public sealed class BookRepositoryTests(IntegrationTestWebAppFactory factory) : BookIntegrationTest(factory)
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnBook_WhenBookExists()
    {
        // Arrange
        var book = Book.Create("Spider-Man", "Amazing Spider-Man", "9780785123456");
        BookRepository.Add(book);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await BookRepository.GetByIdAsync(book.Id);

        // Assert
        result.Should().NotBeNull();
        Guard.Against.Null(result);
        result.Id.Should().Be(book.Id);
        result.Serie.Should().Be("Spider-Man");
        result.Title.Should().Be("Amazing Spider-Man");
        result.ISBN.Should().Be("9780785123456");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenBookDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.CreateVersion7();

        // Act
        var result = await BookRepository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowException_WhenIdIsDefault()
    {
        // Arrange
        var defaultId = Guid.Empty;

        // Act
        var action = async () => await BookRepository.GetByIdAsync(defaultId);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldIncludeReadingDates_WhenBookHasReadingDates()
    {
        // Arrange
        var book = Book.Create("X-Men", "Uncanny X-Men", "9780785134567");
        book.AddReadingDate(DateTime.UtcNow, "First reading");
        book.AddReadingDate(DateTime.UtcNow.AddDays(-30), "Re-read");
        BookRepository.Add(book);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await BookRepository.GetByIdAsync(book.Id);

        // Assert
        result.Should().NotBeNull();
        Guard.Against.Null(result);
        result.ReadingDates.Should().HaveCount(2);
    }

    [Fact]
    public async Task Add_ShouldAddBook_WhenBookIsValid()
    {
        // Arrange
        var book = Book.Create("Batman", "The Dark Knight Returns", "9780785145678");

        // Act
        BookRepository.Add(book);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert
        BookRepository.Count().Should().Be(1);
        var savedBook = await BookRepository.GetByIdAsync(book.Id);
        Guard.Against.Null(savedBook);
        savedBook.Serie.Should().Be("Batman");
        savedBook.Title.Should().Be("The Dark Knight Returns");
        savedBook.ISBN.Should().Be("9780785145678");
    }

    [Fact]
    public async Task Add_ShouldThrowException_WhenAddBookWithSameIdTwice()
    {
        // Arrange
        var book = Book.Create("Superman", "Man of Steel", "9780785156789");
        BookRepository.Add(book);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
        BookRepository.Add(book);
        var action = async () => await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act && Assert
        Guard.Against.Null(action);
        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Update_ShouldUpdateBook_WhenBookExists()
    {
        // Arrange
        var book = Book.Create("Avengers", "The Avengers", "9780785167890", 1);
        BookRepository.Add(book);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        book.Update("Avengers", "New Avengers", "9780785167890", 2, "http://example.com/image.jpg");
        BookRepository.Update(book);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert
        BookRepository.Count().Should().Be(1);
        var savedBook = await BookRepository.GetByIdAsync(book.Id);
        Guard.Against.Null(savedBook);
        savedBook.Title.Should().Be("New Avengers");
        savedBook.VolumeNumber.Should().Be(2);
        savedBook.ImageLink.Should().Be("http://example.com/image.jpg");
    }

    [Fact]
    public async Task Remove_ShouldRemoveBook_WhenBookExists()
    {
        // Arrange
        var book = Book.Create("Wonder Woman", "Wonder Woman Vol 1", "9780785178901");
        BookRepository.Add(book);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        BookRepository.Remove(book);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert
        BookRepository.Count().Should().Be(0);
        var savedBook = await BookRepository.GetByIdAsync(book.Id);
        savedBook.Should().BeNull();
    }

    [Fact]
    public async Task Count_ShouldReturnZero_WhenNoBooksExist()
    {
        // Act
        var count = BookRepository.Count();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task Count_ShouldReturnCorrectCount_WhenBooksExist()
    {
        // Arrange
        var book1 = Book.Create("Flash", "The Flash", "9780785189012");
        var book2 = Book.Create("Green Lantern", "Green Lantern Corps", "9780785190123");
        var book3 = Book.Create("Aquaman", "Aquaman", "9780785191234");
        BookRepository.Add(book1);
        BookRepository.Add(book2);
        BookRepository.Add(book3);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var count = BookRepository.Count();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnAllBooks_WhenBooksExist()
    {
        // Arrange
        var book1 = Book.Create("Justice League", "Justice League Vol 1", "9780785192345");
        var book2 = Book.Create("Teen Titans", "Teen Titans Vol 1", "9780785193456");
        var book3 = Book.Create("Doom Patrol", "Doom Patrol Vol 1", "9780785194567");
        BookRepository.Add(book1);
        BookRepository.Add(book2);
        BookRepository.Add(book3);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var list = await BookRepository.ListAsync();

        // Assert
        BookRepository.Count().Should().Be(3);
        list.Should().HaveCount(3);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnEmptyList_WhenNoBooksExist()
    {
        // Act
        var list = await BookRepository.ListAsync();

        // Assert
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAsync_ShouldIncludeReadingDates_WhenBooksHaveReadingDates()
    {
        // Arrange
        var book1 = Book.Create("Fantastic Four", "Fantastic Four Vol 1", "9780785195678");
        book1.AddReadingDate(DateTime.UtcNow, "Great read");
        var book2 = Book.Create("Guardians of the Galaxy", "Guardians Vol 1", "9780785196789");
        book2.AddReadingDate(DateTime.UtcNow, "Amazing story");
        book2.AddReadingDate(DateTime.UtcNow.AddDays(-10), "Re-read");
        BookRepository.Add(book1);
        BookRepository.Add(book2);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var list = await BookRepository.ListAsync();

        // Assert
        list.Should().HaveCount(2);
        var firstBook = list.FirstOrDefault(b => b.Id == book1.Id);
        var secondBook = list.FirstOrDefault(b => b.Id == book2.Id);
        Guard.Against.Null(firstBook);
        Guard.Against.Null(secondBook);
        firstBook.ReadingDates.Should().HaveCount(1);
        secondBook.ReadingDates.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListAsyncWithCancellationToken_ShouldReturnAllBooks_WhenBooksExist()
    {
        // Arrange
        var book1 = Book.Create("Black Panther", "Black Panther Vol 1", "9780785197890");
        var book2 = Book.Create("Captain Marvel", "Captain Marvel Vol 1", "9780785198901");
        BookRepository.Add(book1);
        BookRepository.Add(book2);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var list = await BookRepository.ListAsync();

        // Assert
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIsbnAsync_ShouldReturnBook_WhenIsbnExists()
    {
        // Arrange
        var book = Book.Create("Daredevil", "Daredevil Vol 1", "9780785199012");
        BookRepository.Add(book);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await BookRepository.GetByIsbnAsync("978-0-7851-9901-2");

        // Assert
        result.Should().NotBeNull();
        Guard.Against.Null(result);
        result.Id.Should().Be(book.Id);
        result.ISBN.Should().Be("9780785199012");
    }

    [Fact]
    public async Task GetByIsbnAsync_ShouldReturnBook_WhenIsbnIsNormalized()
    {
        // Arrange
        var book = Book.Create("Punisher", "The Punisher", "9780785200123");
        BookRepository.Add(book);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await BookRepository.GetByIsbnAsync("978-0-7852-0012-3");

        // Assert
        result.Should().NotBeNull();
        Guard.Against.Null(result);
        result.ISBN.Should().Be("9780785200123");
    }

    [Fact]
    public async Task GetByIsbnAsync_ShouldReturnNull_WhenIsbnDoesNotExist()
    {
        // Arrange
        var book = Book.Create("Hawkeye", "Hawkeye Vol 1", "9780785201234");
        BookRepository.Add(book);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await BookRepository.GetByIsbnAsync("9780000000000");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIsbnAsync_ShouldThrowArgumentException_WhenIsbnIsNull()
    {
        // Act
        var action = async () => await BookRepository.GetByIsbnAsync(null!);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("ISBN cannot be null or empty. (Parameter 'isbn')");
    }

    [Fact]
    public async Task GetByIsbnAsync_ShouldThrowArgumentException_WhenIsbnIsEmpty()
    {
        // Act
        var action = async () => await BookRepository.GetByIsbnAsync(string.Empty);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("ISBN cannot be null or empty. (Parameter 'isbn')");
    }

    [Fact]
    public async Task GetByIsbnAsync_ShouldThrowArgumentException_WhenIsbnIsWhitespace()
    {
        // Act
        var action = async () => await BookRepository.GetByIsbnAsync("   ");

        // Assert
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("ISBN cannot be null or empty. (Parameter 'isbn')");
    }

    [Fact]
    public async Task GetByIsbnAsync_ShouldHandleIsbn10Format_WhenProvided()
    {
        // Arrange
        var book = Book.Create("Iron Man", "Iron Man Vol 1", "043965548X");
        BookRepository.Add(book);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await BookRepository.GetByIsbnAsync("0-439-65548-X");

        // Assert
        result.Should().NotBeNull();
        Guard.Against.Null(result);
        result.ISBN.Should().Be("043965548X");
    }

    [Fact]
    public async Task GetByIsbnAsync_ShouldHandleIsbnWithSpaces_WhenProvided()
    {
        // Arrange
        var book = Book.Create("Thor", "Thor Vol 1", "9780785202345");
        BookRepository.Add(book);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await BookRepository.GetByIsbnAsync("978 0 7852 0234 5");

        // Assert
        result.Should().NotBeNull();
        Guard.Against.Null(result);
        result.ISBN.Should().Be("9780785202345");
    }

    [Fact]
    public async Task GetByIsbnAsync_ShouldBeCaseInsensitive_WhenIsbn10HasX()
    {
        // Arrange
        var book = Book.Create("Loki", "Loki Vol 1", "043965548X");
        BookRepository.Add(book);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await BookRepository.GetByIsbnAsync("043965548x");

        // Assert
        result.Should().NotBeNull();
        Guard.Against.Null(result);
        result.ISBN.Should().Be("043965548X");
    }
}
