using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class ReturnLinksTests
{
    private static readonly Guid BookId = Guid.Parse("0199e0a4-5f6b-7c8d-9e0f-112233445566");

    [Theory]
    [InlineData("/reading")]
    [InlineData("/")]
    [InlineData("/libraries/0199e0a4-5f6b-7c8d-9e0f-112233445566/serie?name=Blacksad")]
    public void Local_Should_KeepThePath_WhenItIsAPathOfTheSite(string from)
    {
        ReturnLinks.Local(from).Should().Be(from);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("reading")]
    [InlineData("https://evil.example/")]
    [InlineData("//evil.example/")]
    [InlineData("/\\evil.example/")]
    [InlineData("/reading\r\nSet-Cookie: x")]
    public void Local_Should_ReturnNull_WhenThePathLeavesTheSite(string? from)
    {
        ReturnLinks.Local(from).Should().BeNull();
    }

    [Fact]
    public void With_Should_AddTheEscapedOrigin_WhenItIsAPathOfTheSite()
    {
        ReturnLinks.Book(BookId, "/reading").Should().Be($"/books/{BookId}?from=%2Freading");
        ReturnLinks.Read(BookId, $"/books/{BookId}?from=%2Freading").Should().Be($"/books/{BookId}/read?from=%2Fbooks%2F{BookId}%3Ffrom%3D%252Freading");
        ReturnLinks.With($"/books/{BookId}/read?mode=isbn", "/reading").Should().Be($"/books/{BookId}/read?mode=isbn&from=%2Freading");
    }

    [Fact]
    public void With_Should_LeaveTheOriginOut_WhenItLeavesTheSite()
    {
        ReturnLinks.Book(BookId, "https://evil.example/").Should().Be($"/books/{BookId}");
        ReturnLinks.Read(BookId, null).Should().Be($"/books/{BookId}/read");
    }
}
