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
        var dto = new BookSummaryDto { Id = Guid.CreateVersion7(), Serie = "Blacksad", Title = "Âme rouge", IsbnScanState = IsbnScanState.NotFound };

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
}
