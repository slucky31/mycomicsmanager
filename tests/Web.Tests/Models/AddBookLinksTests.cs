using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class AddBookLinksTests
{
    private static readonly Guid LibraryId = Guid.Parse("0199e0a4-5f6b-7c8d-9e0f-112233445566");

    [Fact]
    public void Steps_Should_KeepTheLibrary_WhenOneIsKnown()
    {
        AddBookLinks.Scan(LibraryId).Should().Be($"/books/add/scan?libraryId={LibraryId}");
        AddBookLinks.TypeIsbn(LibraryId).Should().Be($"/books/add/manual?libraryId={LibraryId}");
        AddBookLinks.EnterDetails(LibraryId).Should().Be($"/books/add/form?libraryId={LibraryId}");
        AddBookLinks.LookUp("9782413015406", LibraryId).Should().Be($"/books/add/form?isbn=9782413015406&libraryId={LibraryId}");
        AddBookLinks.Exit(LibraryId).Should().Be($"/libraries/{LibraryId}");
    }

    [Fact]
    public void Steps_Should_LeaveTheLibraryOut_WhenNoneIsKnown()
    {
        AddBookLinks.Scan(null).Should().Be("/books/add/scan");
        AddBookLinks.LookUp("2-205-05005-X", null).Should().Be("/books/add/form?isbn=2-205-05005-X");
        AddBookLinks.Exit(null).Should().Be("/libraries/list");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void ParseLibraryId_Should_ReturnNull_WhenTheValueIsNotAnId(string? value)
    {
        AddBookLinks.ParseLibraryId(value).Should().BeNull();
    }

    [Fact]
    public void ParseLibraryId_Should_ReturnTheId_WhenTheValueIsAnId()
    {
        AddBookLinks.ParseLibraryId(LibraryId.ToString()).Should().Be(LibraryId);
    }
}
