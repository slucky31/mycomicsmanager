using AwesomeAssertions;
using Domain.Books;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class EditBookViewModelTests
{
    private static readonly Guid LibraryId = Guid.CreateVersion7();

    [Fact]
    public void From_Should_DescribeABook_WithItsReadings_MostRecentFirst()
    {
        var book = PhysicalBook.Create(new BookMetadata("Extremity", "Extremity", "9782413015406", 1, "/covers/extremity.webp"), LibraryId).Value!;
        book.AddReadingDate(new DateTime(2024, 8, 2, 0, 0, 0, DateTimeKind.Utc), 5);
        book.AddReadingDate(new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc), 4);

        var view = EditBookViewModel.From(book);

        view.Should().BeEquivalentTo(new
        {
            book.Id,
            Title = "Extremity",
            ImageLink = "/covers/extremity.webp",
            IsDigital = false,
            Subtitle = "Extremity · Volume 1",
            BookHref = $"/books/{book.Id}",
            CoverAlt = "Cover of Extremity",
            ReadingsCountLabel = "2 readings",
        });
        view.Readings.Select(r => (r.Date, r.Rating, r.DeleteLabel)).Should().Equal(
            ("12/03/2026", 4, "Delete the reading of 12/03/2026"),
            ("02/08/2024", 5, "Delete the reading of 02/08/2024"));
        view.LibraryLabel("Read Books").Should().Be("Read Books · Physical");
    }

    [Fact]
    public void From_Should_CountASingleReading_AndNameADigitalLibrary_WhenItsNameIsUnknown()
    {
        var book = DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", null, 0), LibraryId, "/data/A/b.cbz", 1).Value!;
        book.AddReadingDate(new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc), 3);

        var view = EditBookViewModel.From(book);

        view.Subtitle.Should().Be("Âme rouge");
        view.ReadingsCountLabel.Should().Be("1 reading");
        view.LibraryLabel(null).Should().Be("Library · Digital");
    }
}
