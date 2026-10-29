using AwesomeAssertions;
using Domain.Books;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class BookDetailViewModelTests
{
    private static readonly Guid LibraryId = Guid.CreateVersion7();

    private static DigitalBook Digital(BookMetadata metadata, long fileSize = 1) =>
        DigitalBook.Create(metadata, LibraryId, "/data/A/b.cbz", fileSize).Value!;

    private static PhysicalBook Physical(BookMetadata metadata) => PhysicalBook.Create(metadata, LibraryId).Value!;

    [Fact]
    public void From_Should_DescribeADigitalBook_WithItsLinksAndDetails()
    {
        var book = Digital(new BookMetadata("Blacksad", "Âme rouge", "9782205050059", 4, "/covers/b.webp", "Díaz Canales, Guarnido", "Dargaud", new DateOnly(2005, 11, 25), 56), 52_428_800L);
        book.CreatedOnUtc = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);

        var view = BookDetailViewModel.From(book);

        view.Should().BeEquivalentTo(new
        {
            book.Id,
            LibraryId,
            Title = "Âme rouge",
            ImageLink = "/covers/b.webp",
            Authors = "Díaz Canales, Guarnido",
            IsDigital = true,
            SerieAndVolume = "Blacksad · Volume 4",
            FormatLabel = "Digital · 56 pages",
            ActionsDetails = "Volume 4 · Digital",
            Isbn = "9782205050059",
            IsbnScanState = IsbnScanState.HasIsbn,
            IsbnScanNotice = (string?)null,
            CoverAlt = "Cover of Âme rouge",
            ActionsLabel = "Actions for Âme rouge",
            LibraryHref = $"/libraries/{LibraryId}",
            DownloadHref = $"/api/books/{book.Id}/download",
        });
        view.Details.Should().Equal(
            new BookDetailField("Volume", "4"),
            new BookDetailField("Publisher", "Dargaud"),
            new BookDetailField("Published", "25/11/2005"),
            new BookDetailField("Pages", "56"),
            new BookDetailField("File size", "50 MB"),
            new BookDetailField("Added", "10/10/2026"));
    }

    [Fact]
    public void From_Should_LeaveOutTheMissingDetails_WhenThePaperBookHasNone()
    {
        var view = BookDetailViewModel.From(Physical(new BookMetadata("Extremity", "Extremity", "9782413015406", 0)));

        view.Should().BeEquivalentTo(new
        {
            IsDigital = false,
            SerieAndVolume = (string?)null,
            FormatLabel = (string?)null,
            ActionsDetails = "Paper",
            Isbn = "9782413015406",
            Details = Array.Empty<BookDetailField>(),
            Readings = Array.Empty<BookReadingEntry>(),
            LatestRating = 0,
            HasBeenRead = false,
        });
    }

    [Theory]
    [InlineData("Extremity", "Extremity")]
    [InlineData(" extremity ", "Extremity")]
    public void From_Should_HideTheSerie_WhenItIsTheTitle(string serie, string title)
    {
        var view = BookDetailViewModel.From(Physical(new BookMetadata(serie, title, "9782413015406", 1)));

        view.SerieAndVolume.Should().Be("Volume 1");
    }

    [Fact]
    public void From_Should_ShowTheLatestRatingAndTheReadingsNewestFirst_WhenTheBookWasRead()
    {
        var book = Physical(new BookMetadata("Extremity", "Extremity", "9782413015406"));
        book.AddReadingDate(new DateTime(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc), 5);
        book.AddReadingDate(new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc), 3);
        book.AddReadingDate(new DateTime(2024, 3, 2, 0, 0, 0, DateTimeKind.Utc), 4);

        var view = BookDetailViewModel.From(book);

        view.LatestRating.Should().Be(3);
        view.LatestRatingLabel.Should().Be("Rated 3 out of 5");
        view.HasBeenRead.Should().BeTrue();
        view.Readings.Select(r => (r.Date, r.Rating, r.RatingLabel)).Should().Equal(
            ("21/08/2026", 3, "Rated 3 out of 5"),
            ("05/01/2025", 5, "Rated 5 out of 5"),
            ("02/03/2024", 4, "Rated 4 out of 5"));
    }

    [Fact]
    public void From_Should_OfferToRead_WhenTheDigitalBookWasNeverOpened()
    {
        var view = BookDetailViewModel.From(Digital(new BookMetadata("Blacksad", "Âme rouge", null, NumberOfPages: 1)));

        view.FormatLabel.Should().Be("Digital · 1 page");
        view.ReadLabel.Should().Be("Read");
        view.ReadProgressLabel.Should().BeNull();
        view.ReadProgressPercent.Should().BeNull();
        view.CanStartOver.Should().BeFalse();
    }

    [Theory]
    [InlineData(67, 188, "Page 68 of 188", 36)]
    [InlineData(300, 188, "Page 188 of 188", 100)]
    public void From_Should_OfferToContinueReading_WhenTheReaderRecordedAPage(int lastReadPage, int pages, string progress, int percent)
    {
        var book = Digital(new BookMetadata("Blacksad", "Âme rouge", null, NumberOfPages: pages));
        book.UpdateReadingProgress(lastReadPage, DateTime.UtcNow);

        var view = BookDetailViewModel.From(book);

        view.ReadLabel.Should().Be("Continue reading");
        view.ReadProgressLabel.Should().Be(progress);
        view.ReadProgressPercent.Should().Be(percent);
        view.CanStartOver.Should().BeTrue();
    }

    [Fact]
    public void From_Should_ShowThePageWithoutProgress_WhenTheNumberOfPagesIsUnknown()
    {
        var book = Digital(new BookMetadata("Blacksad", "Âme rouge", null));
        book.UpdateReadingProgress(9, DateTime.UtcNow);

        var view = BookDetailViewModel.From(book);

        view.ReadProgressLabel.Should().Be("Page 10");
        view.ReadProgressPercent.Should().BeNull();
        view.FormatLabel.Should().Be("Digital");
    }

    [Fact]
    public void From_Should_OfferToScanThePages_WhenTheyWereNeverScanned()
    {
        var view = BookDetailViewModel.From(Digital(new BookMetadata("Blacksad", "Âme rouge", null)));

        view.Isbn.Should().BeNull();
        view.IsbnScanState.Should().Be(IsbnScanState.NotScanned);
        view.IsbnScanNotice.Should().Be("The pages were not scanned for an ISBN yet.");
        view.ScanIsbnLabel.Should().Be("Scan the pages");
    }

    [Fact]
    public void From_Should_OfferToScanAgain_WhenThePagesShowedNoIsbn()
    {
        var book = Digital(new BookMetadata("Blacksad", "Âme rouge", null));
        book.RecordIsbnScan([], new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc));

        var view = BookDetailViewModel.From(book);

        view.IsbnScanNotice.Should().Be("No ISBN found in the pages (scanned on 10/10/2026).");
        view.ScanIsbnLabel.Should().Be("Scan again");
    }

    [Fact]
    public void From_Should_ListTheCandidates_WhenSeveralIsbnsWereRead()
    {
        var book = Digital(new BookMetadata("Blacksad", "Âme rouge", null));
        book.RecordIsbnScan(["9782800112343", "2205056174"], DateTime.UtcNow);

        var view = BookDetailViewModel.From(book);

        view.IsbnScanState.Should().Be(IsbnScanState.Candidates);
        view.IsbnCandidates.Should().Equal("9782800112343", "2205056174");
        view.IsbnScanNotice.Should().BeNull();
    }

    [Fact]
    public void From_Should_Throw_WhenTheBookIsNull()
    {
        var act = () => BookDetailViewModel.From(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
