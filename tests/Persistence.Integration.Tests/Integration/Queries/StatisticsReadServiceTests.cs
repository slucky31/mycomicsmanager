using Base.Integration.Tests;
using Domain.Books;
using Domain.Libraries;

namespace Persistence.Tests.Integration.Queries;

[Collection("DatabaseCollectionTests")]
public class StatisticsReadServiceTests(IntegrationTestWebAppFactory factory) : StatisticsReadServiceIntegrationTest(factory)
{
    private static readonly DateTime s_readingsFrom = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static int s_isbnSequence;

    private static PhysicalBook CreateBook(Guid libraryId, string serie, int volumeNumber = 1, int? numberOfPages = null)
        => PhysicalBook.Create(
            new BookMetadata(serie, $"{serie} {volumeNumber}", $"97810000{Interlocked.Increment(ref s_isbnSequence):D5}", volumeNumber, NumberOfPages: numberOfPages),
            libraryId).Value!;

    private async Task<Library> CreateLibraryAsync(Guid userId, LibraryBookType bookType = LibraryBookType.Physical)
    {
        var library = Library.Create($"Library {Guid.CreateVersion7():N}", "#000000", "book", bookType, userId).Value!;
        Context.Libraries.Add(library);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
        return library;
    }

    private async Task SeedAsync(params Book[] books)
    {
        foreach (var book in books)
        {
            BookRepository.Add(book);
        }
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GetStatisticsAsync_Should_ReturnZeros_WhenUserHasNoBook()
    {
        // Act
        var result = await StatisticsReadService.GetStatisticsAsync(
            DefaultLibrary.UserId, null, s_readingsFrom, TestContext.Current.CancellationToken);

        // Assert
        result.TotalBooks.Should().Be(0);
        result.TotalSeries.Should().Be(0);
        result.TotalLibraries.Should().Be(1);
        result.ReadBooks.Should().Be(0);
        result.TotalReadings.Should().Be(0);
        result.AverageRating.Should().BeNull();
        result.TotalPages.Should().Be(0);
        result.DigitalStorageBytes.Should().Be(0);
        result.ReadingsPerMonth.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatisticsAsync_Should_AggregateUserBooks_WhenBooksAreInSeveralLibraries()
    {
        // Arrange
        var digitalLibrary = await CreateLibraryAsync(DefaultLibrary.UserId, LibraryBookType.Digital);
        var readBook = CreateBook(DefaultLibrary.Id, "Naruto", 1, numberOfPages: 200);
        readBook.AddReadingDate(new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc), 4);
        readBook.AddReadingDate(new DateTime(2025, 3, 20, 0, 0, 0, DateTimeKind.Utc), 2);
        var unreadBook = CreateBook(DefaultLibrary.Id, "Naruto", 2, numberOfPages: 180);
        var digitalBook = DigitalBook.Create(
            new BookMetadata("One Piece", "One Piece 1", null), digitalLibrary.Id, "/data/one-piece-1.cbz", 2048).Value!;
        digitalBook.AddReadingDate(new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc), 3);
        await SeedAsync(readBook, unreadBook, digitalBook);

        // Act
        var result = await StatisticsReadService.GetStatisticsAsync(
            DefaultLibrary.UserId, null, s_readingsFrom, TestContext.Current.CancellationToken);

        // Assert
        result.TotalBooks.Should().Be(3);
        result.TotalSeries.Should().Be(2);
        result.TotalLibraries.Should().Be(2);
        result.ReadBooks.Should().Be(2);
        result.UnreadBooks.Should().Be(1);
        result.TotalReadings.Should().Be(3);
        result.AverageRating.Should().Be(3);
        result.TotalPages.Should().Be(380);
        result.DigitalStorageBytes.Should().Be(2048);
        result.ReadingsPerMonth.Should().BeEquivalentTo(new[]
        {
            new { Year = 2025, Month = 3, Count = 2 },
            new { Year = 2025, Month = 5, Count = 1 }
        });
    }

    [Fact]
    public async Task GetStatisticsAsync_Should_OnlyCountSelectedLibrary_WhenLibraryIdIsProvided()
    {
        // Arrange
        var otherLibrary = await CreateLibraryAsync(DefaultLibrary.UserId);
        await SeedAsync(
            CreateBook(DefaultLibrary.Id, "Naruto", 1),
            CreateBook(otherLibrary.Id, "Bleach", 1),
            CreateBook(otherLibrary.Id, "Bleach", 2));

        // Act
        var result = await StatisticsReadService.GetStatisticsAsync(
            DefaultLibrary.UserId, otherLibrary.Id, s_readingsFrom, TestContext.Current.CancellationToken);

        // Assert
        result.TotalBooks.Should().Be(2);
        result.TotalSeries.Should().Be(1);
        result.TotalLibraries.Should().Be(1);
    }

    [Fact]
    public async Task GetStatisticsAsync_Should_IgnoreOtherUsersBooks_WhenComputingStatistics()
    {
        // Arrange
        var otherUserLibrary = await CreateLibraryAsync(Guid.CreateVersion7());
        var otherUserBook = CreateBook(otherUserLibrary.Id, "Bleach", 1, numberOfPages: 100);
        otherUserBook.AddReadingDate(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), 5);
        await SeedAsync(CreateBook(DefaultLibrary.Id, "Naruto", 1), otherUserBook);

        // Act
        var result = await StatisticsReadService.GetStatisticsAsync(
            DefaultLibrary.UserId, null, s_readingsFrom, TestContext.Current.CancellationToken);

        // Assert
        result.TotalBooks.Should().Be(1);
        result.TotalLibraries.Should().Be(1);
        result.TotalReadings.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.ReadingsPerMonth.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatisticsAsync_Should_ExcludeReadingsBeforeStartDate_WhenGroupingByMonth()
    {
        // Arrange
        var book = CreateBook(DefaultLibrary.Id, "Naruto", 1);
        book.AddReadingDate(new DateTime(2024, 12, 31, 23, 59, 0, DateTimeKind.Utc), 4);
        book.AddReadingDate(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), 4);
        await SeedAsync(book);

        // Act
        var result = await StatisticsReadService.GetStatisticsAsync(
            DefaultLibrary.UserId, null, s_readingsFrom, TestContext.Current.CancellationToken);

        // Assert
        result.TotalReadings.Should().Be(2);
        result.ReadingsPerMonth.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Year = 2025, Month = 1, Count = 1 });
    }
}
