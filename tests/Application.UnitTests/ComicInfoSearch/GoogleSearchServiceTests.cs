using Application.ComicInfoSearch;
using NSubstitute;

namespace Application.UnitTests.ComicInfoSearch;

public class GoogleSearchServiceTests
{
    private readonly ICustomSearchApiClient _searchApiClientMock;
    private readonly GoogleSearchService _service;

    public GoogleSearchServiceTests()
    {
        _searchApiClientMock = Substitute.For<ICustomSearchApiClient>();
        _service = new GoogleSearchService(_searchApiClientMock);
    }

    [Fact]
    public void Constructor_Should_CreateInstance_WhenClientProvided()
    {
        // Assert
        _service.Should().NotBeNull();
    }

    [Fact]
    public void SearchLinkFromKeywordAndPattern_Should_ReturnFirstLink_WhenResultsFound()
    {
        // Arrange
        var keyword = "test keyword";
        var expectedLink = "https://example.com/result1";
        var links = new List<string> { expectedLink, "https://example.com/result2" };
        _searchApiClientMock.ExecuteSearch(keyword, 1).Returns(links);

        // Act
        var result = _service.SearchLinkFromKeywordAndPattern(keyword);

        // Assert
        result.Should().Be(expectedLink);
        _searchApiClientMock.Received(1).ExecuteSearch(keyword, 1);
    }

    [Fact]
    public void SearchLinkFromKeywordAndPattern_Should_ReturnEmptyString_WhenNoResultsFound()
    {
        // Arrange
        var keyword = "test keyword";
        _searchApiClientMock.ExecuteSearch(keyword, 1).Returns((IList<string>?)null);

        // Act
        var result = _service.SearchLinkFromKeywordAndPattern(keyword);

        // Assert
        result.Should().BeEmpty();
        _searchApiClientMock.Received(1).ExecuteSearch(keyword, 1);
    }

    [Fact]
    public void SearchLinkFromKeywordAndPattern_Should_ReturnEmptyString_WhenEmptyListReturned()
    {
        // Arrange
        var keyword = "test keyword";
        _searchApiClientMock.ExecuteSearch(keyword, 1).Returns(new List<string>());
        _searchApiClientMock.ExecuteSearch(keyword, 11).Returns((IList<string>?)null);

        // Act
        var result = _service.SearchLinkFromKeywordAndPattern(keyword);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SearchLinkFromKeywordAndPattern_Should_PaginateThroughResults_WhenEmptyPagesReturned()
    {
        // Arrange
        var keyword = "test keyword";
        var expectedLink = "https://example.com/found";

        // First page returns empty list, second page returns results
        _searchApiClientMock.ExecuteSearch(keyword, 1).Returns(new List<string>());
        _searchApiClientMock.ExecuteSearch(keyword, 11).Returns(new List<string> { expectedLink });

        // Act
        var result = _service.SearchLinkFromKeywordAndPattern(keyword);

        // Assert
        result.Should().Be(expectedLink);
        _searchApiClientMock.Received(1).ExecuteSearch(keyword, 1);
        _searchApiClientMock.Received(1).ExecuteSearch(keyword, 11);
    }

    [Fact]
    public void SearchLinkFromKeywordAndPattern_Should_StopPaginating_WhenNullReturned()
    {
        // Arrange
        var keyword = "test keyword";

        // First page returns empty list, second page returns null (no more results)
        _searchApiClientMock.ExecuteSearch(keyword, 1).Returns(new List<string>());
        _searchApiClientMock.ExecuteSearch(keyword, 11).Returns(new List<string>());
        _searchApiClientMock.ExecuteSearch(keyword, 21).Returns((IList<string>?)null);

        // Act
        var result = _service.SearchLinkFromKeywordAndPattern(keyword);

        // Assert
        result.Should().BeEmpty();
        _searchApiClientMock.Received(1).ExecuteSearch(keyword, 1);
        _searchApiClientMock.Received(1).ExecuteSearch(keyword, 11);
        _searchApiClientMock.Received(1).ExecuteSearch(keyword, 21);
    }
}
