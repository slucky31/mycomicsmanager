using Base.Integration.Tests;
using Domain.Books;
using Domain.Libraries;

namespace Persistence.Tests.Integration.Queries;

[Collection("DatabaseCollectionTests")]
public class BookReadServiceTests(IntegrationTestWebAppFactory factory) : BookReadServiceIntegrationTest(factory)
{
    private PhysicalBook CreateBook(string serie, string title, string isbn, int volumeNumber = 1)
        => PhysicalBook.Create(serie, title, isbn, volumeNumber, "", "", "", null, null, DefaultLibrary.Id).Value!;

    private async Task SeedAsync(IEnumerable<PhysicalBook> books)
    {
        foreach (var book in books)
        {
            BookRepository.Add(book);
        }
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    // ── Substring search (trigram index) ────────────────────────────────────

    [Fact]
    public async Task GetPagedByLibraryAsync_Should_MatchSubstring_WhenSearchTermIsInMiddleOfTitle()
    {
        // Arrange
        var book = CreateBook("Marvel", "The Complete Naruto Box Set", "9780000000090");
        await SeedAsync([book]);

        // Act
        var result = await BookReadService.GetPagedByLibraryAsync(
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.IdDesc, "plete");

        // Assert
        result.Items.Should().ContainSingle(b => b.Id == book.Id);
    }

    // ── Case-insensitive search ──────────────────────────────────────────────

    [Fact]
    public async Task GetPagedByLibraryAsync_Should_MatchTitle_CaseInsensitively_WhenSearchTermIsLowercase()
    {
        // Arrange
        var book = CreateBook("Marvel", "NARUTO Vol 1", "9780000000001");
        await SeedAsync([book]);

        // Act
        var result = await BookReadService.GetPagedByLibraryAsync(
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.IdDesc, "naruto");

        // Assert
        result.Items.Should().ContainSingle(b => b.Id == book.Id);
    }

    [Fact]
    public async Task GetPagedByLibraryAsync_Should_MatchSerie_CaseInsensitively_WhenSearchTermIsLowercase()
    {
        // Arrange
        var book = CreateBook("One Piece", "East Blue Arc", "9780000000002");
        await SeedAsync([book]);

        // Act
        var result = await BookReadService.GetPagedByLibraryAsync(
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.IdDesc, "one piece");

        // Assert
        result.Items.Should().ContainSingle(b => b.Id == book.Id);
    }

    [Fact]
    public async Task GetPagedByLibraryAsync_Should_ExcludeNonMatchingBooks_WhenSearchTermProvided()
    {
        // Arrange
        var matching = CreateBook("Naruto", "Naruto Vol 1", "9780000000003");
        var nonMatching = CreateBook("One Piece", "East Blue Arc", "9780000000004");
        await SeedAsync([matching, nonMatching]);

        // Act
        var result = await BookReadService.GetPagedByLibraryAsync(
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.IdDesc, "NARUTO");

        // Assert
        result.Items.Should().ContainSingle(b => b.Id == matching.Id);
    }

    // ── Sort orders ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedByLibraryAsync_Should_OrderByIdDescending_WhenSortOrderIsIdDesc()
    {
        // Arrange — create books in sequence so their v7 GUIDs are ordered
        var book1 = CreateBook("A", "Book A", "9780000000010");
        var book2 = CreateBook("B", "Book B", "9780000000011");
        var book3 = CreateBook("C", "Book C", "9780000000012");
        await SeedAsync([book1, book2, book3]);

        // Act
        var result = await BookReadService.GetPagedByLibraryAsync(
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.IdDesc, null);

        // Assert
        var ids = result.Items!.Select(b => b.Id).ToList();
        ids.Should().ContainInOrder(
            [.. new[] { book1.Id, book2.Id, book3.Id }.OrderByDescending(id => id)]);
    }

    [Fact]
    public async Task GetPagedByLibraryAsync_Should_OrderByIdAscending_WhenSortOrderIsIdAsc()
    {
        // Arrange
        var book1 = CreateBook("A", "Book A", "9780000000013");
        var book2 = CreateBook("B", "Book B", "9780000000014");
        var book3 = CreateBook("C", "Book C", "9780000000015");
        await SeedAsync([book1, book2, book3]);

        // Act
        var result = await BookReadService.GetPagedByLibraryAsync(
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.IdAsc, null);

        // Assert
        var ids = result.Items!.Select(b => b.Id).ToList();
        ids.Should().ContainInOrder(
            [.. new[] { book1.Id, book2.Id, book3.Id }.OrderBy(id => id)]);
    }

    [Fact]
    public async Task GetPagedByLibraryAsync_Should_OrderBySerieAndVolumeAscending_WhenSortOrderIsSerieAndVolumeAsc()
    {
        // Arrange
        var vol2 = CreateBook("Naruto", "Naruto", "9780000000020", volumeNumber: 2);
        var vol1 = CreateBook("Naruto", "Naruto", "9780000000021", volumeNumber: 1);
        var other = CreateBook("Akira", "Akira", "9780000000022", volumeNumber: 1);
        await SeedAsync([vol2, vol1, other]);

        // Act
        var result = await BookReadService.GetPagedByLibraryAsync(
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.SerieAndVolumeAsc, null);

        // Assert — Akira before Naruto, then Vol 1 before Vol 2
        var items = result.Items!.ToList();
        items[0].Id.Should().Be(other.Id);
        items[1].Id.Should().Be(vol1.Id);
        items[2].Id.Should().Be(vol2.Id);
    }

    // ── Paging ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedByLibraryAsync_Should_ReturnCorrectPage_AndSetHasNextPage()
    {
        // Arrange — 5 books, page size 2
        var books = Enumerable.Range(1, 5)
            .Select(i => CreateBook("Serie", $"Title {i}", $"978000000003{i}"))
            .ToList();
        await SeedAsync(books);

        // Act
        var page1 = await BookReadService.GetPagedByLibraryAsync(
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 2, BookSortOrder.IdAsc, null);
        var page2 = await BookReadService.GetPagedByLibraryAsync(
            DefaultLibrary.Id, DefaultLibrary.UserId, 2, 2, BookSortOrder.IdAsc, null);
        var page3 = await BookReadService.GetPagedByLibraryAsync(
            DefaultLibrary.Id, DefaultLibrary.UserId, 3, 2, BookSortOrder.IdAsc, null);

        // Assert
        page1.Items.Should().HaveCount(2);
        page1.HasNextPage.Should().BeTrue();
        page1.Page.Should().Be(1);
        page1.PageSize.Should().Be(2);

        page2.Items.Should().HaveCount(2);
        page2.HasNextPage.Should().BeTrue();

        page3.Items.Should().HaveCount(1);
        page3.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetPagedByLibraryAsync_Should_ReturnTotalCount()
    {
        // Arrange
        var books = Enumerable.Range(1, 4)
            .Select(i => CreateBook("Serie", $"Title {i}", $"978000000004{i}"))
            .ToList();
        await SeedAsync(books);

        // Act
        var result = await BookReadService.GetPagedByLibraryAsync(
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 2, BookSortOrder.IdAsc, null);

        // Assert
        result.TotalCount.Should().Be(4);
    }

    // ── Isolation ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedByLibraryAsync_Should_ReturnEmpty_WhenUserIdDoesNotMatch()
    {
        // Arrange
        var book = CreateBook("One Piece", "Vol 1", "9780000000050");
        await SeedAsync([book]);
        var otherUserId = Guid.CreateVersion7();

        // Act
        var result = await BookReadService.GetPagedByLibraryAsync(
            DefaultLibrary.Id, otherUserId, 1, 24, BookSortOrder.IdDesc, null);

        // Assert
        result.Items.Should().BeEmpty();
    }

    // ── ReadingDate aggregates ────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedByLibraryAsync_Should_ReturnCorrectReadingDateAggregates_WhenBookHasMultipleReadingDates()
    {
        // Arrange
        var book = CreateBook("Berserk", "Vol 1", "9780000000060");
        BookRepository.Add(book);
        var olderDate = new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var newerDate = new DateTime(2025, 6, 20, 0, 0, 0, DateTimeKind.Utc);
        var olderEntry = book.AddReadingDate(olderDate, rating: 3);
        var newerEntry = book.AddReadingDate(newerDate, rating: 5);
        BookRepository.AddReadingDate(olderEntry);
        BookRepository.AddReadingDate(newerEntry);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await BookReadService.GetPagedByLibraryAsync(
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.IdDesc, null);

        // Assert
        var item = result.Items.Should().ContainSingle(b => b.Id == book.Id).Subject;
        item.ReadCount.Should().Be(2);
        item.LastRead.Should().Be(newerDate);
        item.LastRating.Should().Be(5);
    }
}
