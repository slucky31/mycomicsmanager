using Application.Libraries.Summaries;
using AwesomeAssertions;
using Domain.Libraries;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class LibraryCardViewModelTests
{
    private static LibraryCardViewModel Card(string name, LibraryBookType bookType, int bookCount, params string[] covers) =>
        LibraryCardViewModel.From(new LibrarySummary(Guid.CreateVersion7(), name, "#2E7D32", "Star", bookType, bookCount) { Covers = covers });

    [Fact]
    public void From_Should_DescribeAPhysicalLibrary_WithItsLinksAndCovers()
    {
        var id = Guid.CreateVersion7();
        var summary = new LibrarySummary(id, "Read Books", "#2E7D32", "Star", LibraryBookType.Physical, 24) { Covers = ["/c/3.webp", "/c/2.webp", "/c/1.webp"] };

        var card = LibraryCardViewModel.From(summary);

        card.Should().BeEquivalentTo(new
        {
            Id = id,
            Name = "Read Books",
            Color = "#2E7D32",
            Icon = "Star",
            IsDigital = false,
            BookCount = 24,
            Href = $"/libraries/{id}",
            EditHref = $"/libraries/{id}/edit",
            ImportHref = $"/import?libraryId={id}",
            TypeLabel = "Physical",
            CountLabel = "24 books",
            Details = "Physical · 24 books",
            ActionsLabel = "More actions for Read Books",
            ActionsButtonId = $"library-actions-{id}",
        });
        card.Covers.Should().Equal("/c/3.webp", "/c/2.webp", "/c/1.webp");
    }

    [Theory]
    [InlineData(0, "No books yet", "Digital · No books yet")]
    [InlineData(1, "1 book", "Digital · 1 book")]
    public void From_Should_DescribeTheBookCountOfADigitalLibrary(int bookCount, string countLabel, string details)
    {
        var card = Card("MesEbooks", LibraryBookType.Digital, bookCount);

        card.IsDigital.Should().BeTrue();
        card.CountLabel.Should().Be(countLabel);
        card.Details.Should().Be(details);
    }

    [Fact]
    public void From_Should_Throw_WhenTheSummaryIsNull()
    {
        var act = () => LibraryCardViewModel.From(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Subtitle_Should_CountTheLibrariesAndAllTheirBooks()
    {
        LibraryCardViewModel[] libraries = [Card("Read Books", LibraryBookType.Physical, 24), Card("À trier", LibraryBookType.Digital, 24), Card("MesEbooks", LibraryBookType.Digital, 0)];

        LibraryCardViewModel.Subtitle(libraries).Should().Be("3 libraries · 48 books");
    }

    [Fact]
    public void Subtitle_Should_UseTheSingular_WhenThereIsOneLibraryWithOneBook()
    {
        LibraryCardViewModel.Subtitle([Card("Read Books", LibraryBookType.Physical, 1)]).Should().Be("1 library · 1 book");
    }

    [Fact]
    public void Subtitle_Should_Throw_WhenTheLibrariesAreNull()
    {
        var act = () => LibraryCardViewModel.Subtitle(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
