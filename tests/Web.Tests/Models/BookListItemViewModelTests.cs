using Application.Books.List;
using AwesomeAssertions;
using Domain.Books;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class BookListItemViewModelTests
{
    [Fact]
    public void From_Should_KeepTheIsbnScanStateOfTheSummary()
    {
        var dto = new BookSummaryDto { Id = Guid.CreateVersion7(), Serie = "Blacksad", Title = "Âme rouge", IsbnScannedAt = DateTime.UtcNow };

        var viewModel = BookListItemViewModel.From(dto);

        viewModel.Title.Should().Be("Âme rouge");
        viewModel.IsbnScanState.Should().Be(IsbnScanState.NotFound);
    }

    [Fact]
    public void From_Should_TakeTheIsbnScanStateOfADigitalBook_AndItsLatestReading()
    {
        var book = DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", null), Guid.CreateVersion7(), "/data/A/b.cbz", 1).Value!;
        book.RecordIsbnScan(["9782800112343", "2205056174"], DateTime.UtcNow);
        book.AddReadingDate(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 3);
        book.AddReadingDate(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), 5);

        var viewModel = BookListItemViewModel.From(book);

        viewModel.IsbnScanState.Should().Be(IsbnScanState.Candidates);
        viewModel.LastRating.Should().Be(5);
        viewModel.ReadCount.Should().Be(2);
    }

    [Fact]
    public void From_Should_ConsiderThatAPhysicalBookHasItsIsbn()
    {
        var book = PhysicalBook.Create(new BookMetadata("Blacksad", "Âme rouge", "9782800112343"), Guid.CreateVersion7()).Value!;

        BookListItemViewModel.From(book).IsbnScanState.Should().Be(IsbnScanState.HasIsbn);
    }

    [Theory]
    [InlineData("Blacksad", 4, "#4", "Blacksad · #4")]
    [InlineData("Blacksad", 0, null, "Blacksad")]
    [InlineData("", 2, "#2", "#2")]
    public void From_Should_PrepareTheVolumeAndTheSeriesShownOnTheBook(string serie, int volume, string? expectedBadge, string expectedSerie)
    {
        var dto = new BookSummaryDto { Id = Guid.CreateVersion7(), Serie = serie, Title = "Âme rouge", VolumeNumber = volume };

        var viewModel = BookListItemViewModel.From(dto);

        viewModel.VolumeBadge.Should().Be(expectedBadge);
        viewModel.SerieAndVolume.Should().Be(expectedSerie);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, true)]
    public void HasBeenRead_Should_BeTrue_WhenTheBookWasReadAtLeastOnce(int readCount, bool expected) =>
        BookListItemViewModel.From(new BookSummaryDto { Title = "Âme rouge", ReadCount = readCount }).HasBeenRead.Should().Be(expected);
}
