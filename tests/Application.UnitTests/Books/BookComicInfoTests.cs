using Application.Books;
using Domain.Books;

namespace Application.UnitTests.Books;

public class BookComicInfoTests
{
    [Fact]
    public void From_Should_MapTheBookMetadata()
    {
        var book = PhysicalBook.Create(new BookMetadata("Blacksad", "Arctic Nation", "9782205050202", 2,
            Authors: "Juan Diaz Canales", Publishers: "Dargaud", PublishDate: new DateOnly(2003, 3, 7), NumberOfPages: 56),
            Guid.CreateVersion7()).Value!;

        var data = BookComicInfo.From(book);

        data.Series.Should().Be("Blacksad");
        data.Title.Should().Be("Arctic Nation");
        data.Number.Should().Be(2);
        data.Writer.Should().Be("Juan Diaz Canales");
        data.Publisher.Should().Be("Dargaud");
        data.Isbn.Should().Be("9782205050202");
        (data.Year, data.Month, data.Day).Should().Be((2003, 3, 7));
        data.PageCount.Should().Be(56);
        data.Summary.Should().BeNull();
        data.Penciller.Should().BeNull();
    }

    [Fact]
    public void From_Should_LeaveEmptyValuesOut()
    {
        var book = DigitalBook.Create(new BookMetadata("Blacksad", "Arctic Nation", null, 2, Authors: " "),
            Guid.CreateVersion7(), "/library/book.cbz", 1024).Value!;

        var data = BookComicInfo.From(book);

        data.Writer.Should().BeNull();
        data.Publisher.Should().BeNull();
        data.Isbn.Should().BeNull();
        data.Year.Should().BeNull();
    }
}
