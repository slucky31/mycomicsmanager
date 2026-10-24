using Application.Books.Search;
using Application.Interfaces;
using Domain.Books;
using NSubstitute;

namespace Application.UnitTests.Books;

public class SearchBooksQueryHandlerTests
{
    private readonly IBookReadService _bookReadService = Substitute.For<IBookReadService>();
    private readonly SearchBooksQueryHandler _handler;

    public SearchBooksQueryHandlerTests()
    {
        _handler = new SearchBooksQueryHandler(_bookReadService);
    }

    [Theory]
    [InlineData("", 20)]
    [InlineData("   ", 20)]
    [InlineData("astérix", 0)]
    [InlineData("astérix", -1)]
    public async Task Handle_Should_ReturnBadRequest_WhenTermIsBlankOrLimitIsNotPositive(string term, int bookLimit)
    {
        // Act
        var result = await _handler.Handle(new SearchBooksQuery(Guid.CreateVersion7(), term, bookLimit), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
        await _bookReadService.DidNotReceive().SearchAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SearchTheTrimmedTerm_WhenQueryIsValid()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var found = new BookSearchResult([new SerieSearchHit(Guid.CreateVersion7(), "Astérix", 2)], [], 0);
        _bookReadService.SearchAsync(userId, "astérix", 20, Arg.Any<CancellationToken>()).Returns(found);

        // Act
        var result = await _handler.Handle(new SearchBooksQuery(userId, "  astérix ", 20), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(found);
    }
}
