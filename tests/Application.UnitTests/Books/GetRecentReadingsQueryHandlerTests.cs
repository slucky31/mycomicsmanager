using Application.Books.Reading;
using Application.Interfaces;
using Domain.Books;
using Domain.Users;
using NSubstitute;

namespace Application.UnitTests.Books;

public class GetRecentReadingsQueryHandlerTests
{
    private readonly IBookReadService _bookReadService = Substitute.For<IBookReadService>();
    private readonly IRepository<User, Guid> _userRepository = Substitute.For<IRepository<User, Guid>>();
    private readonly GetRecentReadingsQueryHandler _handler;

    public GetRecentReadingsQueryHandlerTests()
    {
        _handler = new GetRecentReadingsQueryHandler(_bookReadService, _userRepository);
    }

    [Theory]
    [InlineData(true, 1, 20)]
    [InlineData(false, 0, 20)]
    [InlineData(false, 1, 0)]
    public async Task Handle_Should_ReturnBadRequest_WhenParametersAreInvalid(bool emptyUserId, int page, int pageSize)
    {
        var query = new GetRecentReadingsQuery(emptyUserId ? Guid.Empty : Guid.CreateVersion7(), page, pageSize);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.BadRequest);
        await _bookReadService.DidNotReceive().GetRecentReadingsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_Should_ReturnThePageOfReadings_WithTheHiddenLibrariesOnlyWhenTheUserShowsThem(bool showHidden)
    {
        var userId = Guid.CreateVersion7();
        var user = User.Create("user@example.com", "auth-id");
        user.UpdateShowHiddenLibraries(showHidden);
        _userRepository.GetByIdAsync(userId).Returns(user);
        var readings = Substitute.For<IPagedList<BookReadingDto>>();
        _bookReadService.GetRecentReadingsAsync(userId, 2, 20, showHidden, Arg.Any<CancellationToken>()).Returns(readings);

        var result = await _handler.Handle(new GetRecentReadingsQuery(userId, 2, 20), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(readings);
    }
}
