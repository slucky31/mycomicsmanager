using Application.Books;
using Application.Books.Reading;
using Application.Books.Search;
using Base.Integration.Tests;
using Domain.Books;
using Domain.Libraries;

namespace Persistence.Tests.Integration.Queries;

[Collection("DatabaseCollectionTests")]
public class BookReadServiceTests(IntegrationTestWebAppFactory factory) : BookReadServiceIntegrationTest(factory)
{
    private PhysicalBook CreateBook(string serie, string title, string isbn, int volumeNumber = 1)
        => PhysicalBook.Create(new BookMetadata(serie, title, isbn, volumeNumber), DefaultLibrary.Id).Value!;

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
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.IdDesc, "plete", TestContext.Current.CancellationToken);

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
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.IdDesc, "naruto", TestContext.Current.CancellationToken);

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
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.IdDesc, "one piece", TestContext.Current.CancellationToken);

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
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.IdDesc, "NARUTO", TestContext.Current.CancellationToken);

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
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.IdDesc, null, TestContext.Current.CancellationToken);

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
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.IdAsc, null, TestContext.Current.CancellationToken);

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
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.SerieAndVolumeAsc, null, TestContext.Current.CancellationToken);

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
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 2, BookSortOrder.IdAsc, null, TestContext.Current.CancellationToken);
        var page2 = await BookReadService.GetPagedByLibraryAsync(
            DefaultLibrary.Id, DefaultLibrary.UserId, 2, 2, BookSortOrder.IdAsc, null, TestContext.Current.CancellationToken);
        var page3 = await BookReadService.GetPagedByLibraryAsync(
            DefaultLibrary.Id, DefaultLibrary.UserId, 3, 2, BookSortOrder.IdAsc, null, TestContext.Current.CancellationToken);

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
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 2, BookSortOrder.IdAsc, null, TestContext.Current.CancellationToken);

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
            DefaultLibrary.Id, otherUserId, 1, 24, BookSortOrder.IdDesc, null, TestContext.Current.CancellationToken);

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
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.IdDesc, null, TestContext.Current.CancellationToken);

        // Assert
        var item = result.Items.Should().ContainSingle(b => b.Id == book.Id).Subject;
        item.ReadCount.Should().Be(2);
        item.LastRead.Should().Be(newerDate);
        item.LastRating.Should().Be(5);
    }

    [Fact]
    public async Task ListIdentitiesByUserAsync_Should_ReturnBooksOfAllUserLibrariesWithLibraryName()
    {
        // Arrange
        var book = CreateBook("Blacksad", "Âme rouge", "9782205055819", 3);
        BookRepository.Add(book);
        var otherUserLibrary = Library.Create("Other", "#000000", "book", LibraryBookType.Physical, Guid.CreateVersion7()).Value!;
        Context.Libraries.Add(otherUserLibrary);
        BookRepository.Add(PhysicalBook.Create(new BookMetadata("Blacksad", "Autre", "9782205055820", 4), otherUserLibrary.Id).Value!);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var identities = await BookReadService.ListIdentitiesByUserAsync(DefaultLibrary.UserId, TestContext.Current.CancellationToken);

        // Assert
        identities.Should().ContainSingle().Which.Should().Be(
            new BookIdentityDto(book.Id, "Blacksad", "Âme rouge", 3, "9782205055819", DefaultLibrary.Name));
    }

    [Fact]
    public async Task ListSerieLocationsAsync_Should_ReturnSerieAndLibraryOfEachBookOfTheUser()
    {
        // Arrange
        var book = CreateBook("Blacksad", "Âme rouge", "9782205055821", 3);
        BookRepository.Add(book);
        var otherUserLibrary = Library.Create("Other", "#000000", "book", LibraryBookType.Physical, Guid.CreateVersion7()).Value!;
        Context.Libraries.Add(otherUserLibrary);
        BookRepository.Add(PhysicalBook.Create(new BookMetadata("Blacksad", "Autre", "9782205055822", 4), otherUserLibrary.Id).Value!);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var locations = await BookReadService.ListSerieLocationsAsync(DefaultLibrary.UserId, TestContext.Current.CancellationToken);

        // Assert
        locations.Should().ContainSingle().Which.Should().Be(new BookSerieLocationDto(DefaultLibrary.Id, "Blacksad"));
    }

    // ── ISBN scan state ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedByLibraryAsync_Should_ReturnTheIsbnScanStateOfEachBook()
    {
        // Arrange
        DigitalBook Digital(string title, string? isbn = null) =>
            DigitalBook.Create(new BookMetadata("Blacksad", title, isbn), DefaultLibrary.Id, $"/data/A/{title}.cbz", 1024).Value!;
        var physical = CreateBook("Blacksad", "Amarillo", "9780000000991");
        var withIsbn = Digital("Arctic Nation", "9782205050196");
        var notScanned = Digital("Âme rouge");
        var candidates = Digital("L'enfer, le silence");
        candidates.RecordIsbnScan(["9782800112343", "2205056174"], DateTime.UtcNow);
        var notFound = Digital("Quelque part entre les ombres");
        notFound.RecordIsbnScan([], DateTime.UtcNow);
        BookRepository.Add(physical);
        BookRepository.Add(withIsbn);
        BookRepository.Add(notScanned);
        BookRepository.Add(candidates);
        BookRepository.Add(notFound);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await BookReadService.GetPagedByLibraryAsync(
            DefaultLibrary.Id, DefaultLibrary.UserId, 1, 24, BookSortOrder.IdAsc, null, TestContext.Current.CancellationToken);

        // Assert
        result.Items.Should().NotBeNull();
        result.Items!.ToDictionary(b => b.Id, b => b.IsbnScanState).Should().BeEquivalentTo(new Dictionary<Guid, IsbnScanState>
        {
            [physical.Id] = IsbnScanState.HasIsbn,
            [withIsbn.Id] = IsbnScanState.HasIsbn,
            [notScanned.Id] = IsbnScanState.NotScanned,
            [candidates.Id] = IsbnScanState.Candidates,
            [notFound.Id] = IsbnScanState.NotFound,
        });
    }

    // ── Search of all the libraries (#1209) ──────────────────────────────────

    [Fact]
    public async Task SearchAsync_Should_FindSeriesAndBooksOfAllTheUserLibraries_WhenSerieMatches()
    {
        // Arrange
        var secondLibrary = Library.Create("Digital", "#000000", "book", LibraryBookType.Physical, DefaultLibrary.UserId).Value!;
        var otherUserLibrary = Library.Create("Other", "#000000", "book", LibraryBookType.Physical, Guid.CreateVersion7()).Value!;
        Context.Libraries.AddRange(secondLibrary, otherUserLibrary);
        var second = CreateBook("Astérix", "La Serpe d'or", "9780000001002", 2);
        var first = CreateBook("Astérix", "Astérix le Gaulois", "9780000001001", 1);
        var elsewhere = PhysicalBook.Create(new BookMetadata("Astérix", "Astérix le Gaulois (numérique)", "9780000001003", 1), secondLibrary.Id).Value!;
        BookRepository.Add(second);
        BookRepository.Add(first);
        BookRepository.Add(elsewhere);
        BookRepository.Add(CreateBook("Tintin", "Tintin au Tibet", "9780000001004", 20));
        BookRepository.Add(PhysicalBook.Create(new BookMetadata("Astérix", "Astérix gladiateur", "9780000001005", 4), otherUserLibrary.Id).Value!);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await BookReadService.SearchAsync(DefaultLibrary.UserId, "astérix", 20, true, TestContext.Current.CancellationToken);

        // Assert
        result.Series.Should().BeEquivalentTo([
            new SerieSearchHit(DefaultLibrary.Id, "Astérix", 2),
            new SerieSearchHit(secondLibrary.Id, "Astérix", 1)]);
        result.BookCount.Should().Be(3);
        result.Books.Select(b => b.Id).Should().Equal(first.Id, elsewhere.Id, second.Id);
        result.Books[0].Should().Be(new BookSearchHit(first.Id, DefaultLibrary.Id, "Astérix le Gaulois", "Astérix", 1, first.ImageLink));
    }

    [Fact]
    public async Task SearchAsync_Should_LeaveOutTheHiddenLibraries_UnlessIncludeHidden()
    {
        // Arrange
        var hiddenLibrary = Library.Create("Hidden", "#000000", "book", LibraryBookType.Physical, DefaultLibrary.UserId).Value!;
        hiddenLibrary.UpdateHidden(true);
        Context.Libraries.Add(hiddenLibrary);
        BookRepository.Add(CreateBook("Astérix", "Astérix le Gaulois", "9780000001021", 1));
        BookRepository.Add(PhysicalBook.Create(new BookMetadata("Astérix", "La Serpe d'or", "9780000001022", 2), hiddenLibrary.Id).Value!);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var withoutHidden = await BookReadService.SearchAsync(DefaultLibrary.UserId, "astérix", 20, false, TestContext.Current.CancellationToken);
        var withHidden = await BookReadService.SearchAsync(DefaultLibrary.UserId, "astérix", 20, true, TestContext.Current.CancellationToken);

        // Assert
        withoutHidden.Series.Select(s => s.LibraryId).Should().Equal(DefaultLibrary.Id);
        withoutHidden.BookCount.Should().Be(1);
        withHidden.Series.Should().HaveCount(2);
        withHidden.BookCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchAsync_Should_KeepTheFirstBooksAndCountThemAll_WhenTitleMatchesMoreThanTheLimit()
    {
        // Arrange
        BookRepository.Add(CreateBook("Astérix", "Astérix le Gaulois", "9780000001011", 1));
        BookRepository.Add(CreateBook("Alix", "Le Dernier Gaulois", "9780000001012", 30));
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await BookReadService.SearchAsync(DefaultLibrary.UserId, "GAULOIS", 1, true, TestContext.Current.CancellationToken);

        // Assert
        result.Series.Should().BeEmpty();
        result.BookCount.Should().Be(2);
        result.Books.Should().ContainSingle().Which.Title.Should().Be("Le Dernier Gaulois");
    }

    // ── Reading page (#1252) ─────────────────────────────────────────────────

    private static DigitalBook CreateDigitalBook(Guid libraryId, string title, int volumeNumber)
        => DigitalBook.Create(new BookMetadata("Blacksad", title, null, volumeNumber), libraryId, $"/library/BD/blacksad-{volumeNumber}.cbz", 1_000).Value!;

    [Fact]
    public async Task ListInProgressAsync_Should_ListTheStartedBooks_TheLastOneReadFirst()
    {
        // Arrange
        var digitalLibrary = Library.Create("BD", "#5C6BC0", "book", LibraryBookType.Digital, DefaultLibrary.UserId).Value!;
        var hiddenLibrary = Library.Create("Hidden", "#000000", "book", LibraryBookType.Digital, DefaultLibrary.UserId).Value!;
        hiddenLibrary.UpdateHidden(true);
        Context.Libraries.AddRange(digitalLibrary, hiddenLibrary);
        var readFirst = CreateDigitalBook(digitalLibrary.Id, "Quelque part entre les ombres", 1);
        readFirst.UpdateReadingProgress(5, new DateTime(2026, 10, 1, 20, 0, 0, DateTimeKind.Utc));
        var readLast = CreateDigitalBook(digitalLibrary.Id, "Arctic Nation", 2);
        readLast.UpdateReadingProgress(3, new DateTime(2026, 10, 20, 20, 0, 0, DateTimeKind.Utc));
        var readBeforeTheTimeWasKept = CreateDigitalBook(digitalLibrary.Id, "Âme rouge", 3);
        readBeforeTheTimeWasKept.UpdateReadingProgress(4, DateTime.UtcNow);
        var notStarted = CreateDigitalBook(digitalLibrary.Id, "L'Enfer, le silence", 4);
        var hidden = CreateDigitalBook(hiddenLibrary.Id, "Amarillo", 5);
        hidden.UpdateReadingProgress(9, DateTime.UtcNow);
        foreach (var book in new[] { readFirst, readLast, readBeforeTheTimeWasKept, notStarted, hidden })
        {
            BookRepository.Add(book);
        }
        Context.Entry(readBeforeTheTimeWasKept).Property(b => b.LastReadAt).CurrentValue = null;
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var withoutHidden = await BookReadService.ListInProgressAsync(DefaultLibrary.UserId, false, TestContext.Current.CancellationToken);
        var withHidden = await BookReadService.ListInProgressAsync(DefaultLibrary.UserId, true, TestContext.Current.CancellationToken);

        // Assert
        withoutHidden.Select(b => b.Id).Should().Equal(readLast.Id, readFirst.Id, readBeforeTheTimeWasKept.Id);
        withoutHidden[0].Should().Be(new BookInProgressDto(readLast.Id, digitalLibrary.Id, "Blacksad", "Arctic Nation", 2, readLast.ImageLink, 3, null,
            new DateTime(2026, 10, 20, 20, 0, 0, DateTimeKind.Utc)));
        withHidden.Select(b => b.Id).Should().Contain(hidden.Id).And.HaveCount(4);
    }

    [Fact]
    public async Task GetRecentReadingsAsync_Should_PageTheReadingsOfTheUser_TheLastOneFirst()
    {
        // Arrange
        var hiddenLibrary = Library.Create("Hidden", "#000000", "book", LibraryBookType.Physical, DefaultLibrary.UserId).Value!;
        hiddenLibrary.UpdateHidden(true);
        Context.Libraries.Add(hiddenLibrary);
        var readTwice = CreateBook("Thorgal", "La Magicienne trahie", "9780000001031", 1);
        var firstReading = readTwice.AddReadingDate(new DateTime(2026, 9, 1, 20, 0, 0, DateTimeKind.Utc), 3);
        var lastReading = readTwice.AddReadingDate(new DateTime(2026, 10, 27, 20, 0, 0, DateTimeKind.Utc), 5);
        var readOnce = CreateBook("Thorgal", "L'Île des mers gelées", "9780000001032", 2);
        var middleReading = readOnce.AddReadingDate(new DateTime(2026, 10, 2, 20, 0, 0, DateTimeKind.Utc), 4);
        var neverRead = CreateBook("Thorgal", "Les Trois Vieillards", "9780000001033", 3);
        var hidden = PhysicalBook.Create(new BookMetadata("Thorgal", "La Galère noire", "9780000001034", 4), hiddenLibrary.Id).Value!;
        var hiddenReading = hidden.AddReadingDate(new DateTime(2026, 10, 28, 20, 0, 0, DateTimeKind.Utc), 2);
        await SeedAsync([readTwice, readOnce, neverRead, hidden]);

        // Act
        var firstPage = await BookReadService.GetRecentReadingsAsync(DefaultLibrary.UserId, 1, 2, false, TestContext.Current.CancellationToken);
        var secondPage = await BookReadService.GetRecentReadingsAsync(DefaultLibrary.UserId, 2, 2, false, TestContext.Current.CancellationToken);
        var withHidden = await BookReadService.GetRecentReadingsAsync(DefaultLibrary.UserId, 1, 10, true, TestContext.Current.CancellationToken);

        // Assert
        firstPage.TotalCount.Should().Be(3);
        firstPage.Items!.Select(r => r.ReadingDateId).Should().Equal(lastReading.Id, middleReading.Id);
        firstPage.Items!.First().Should().Be(new BookReadingDto(lastReading.Id, readTwice.Id, DefaultLibrary.Id, "Thorgal", "La Magicienne trahie", 1,
            readTwice.ImageLink, new DateTime(2026, 10, 27, 20, 0, 0, DateTimeKind.Utc), 5));
        secondPage.Items!.Select(r => r.ReadingDateId).Should().Equal(firstReading.Id);
        withHidden.Items!.Select(r => r.ReadingDateId).Should().StartWith(hiddenReading.Id).And.HaveCount(4);
    }
}
