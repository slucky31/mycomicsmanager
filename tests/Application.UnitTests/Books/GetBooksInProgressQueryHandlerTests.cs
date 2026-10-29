using Application.Books.Reading;
using Application.Interfaces;
using Domain.Books;
using Domain.Users;
using NSubstitute;

namespace Application.UnitTests.Books;

public class GetBooksInProgressQueryHandlerTests
{
    private readonly IBookReadService _bookReadService = Substitute.For<IBookReadService>();
    private readonly IRepository<User, Guid> _userRepository = Substitute.For<IRepository<User, Guid>>();
    private readonly GetBooksInProgressQueryHandler _handler;

    public GetBooksInProgressQueryHandlerTests()
    {
        _handler = new GetBooksInProgressQueryHandler(_bookReadService, _userRepository);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenUserIdIsEmpty()
    {
        var result = await _handler.Handle(new GetBooksInProgressQuery(Guid.Empty), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.BadRequest);
        await _bookReadService.DidNotReceive().ListInProgressAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_Should_ListTheBooksInProgress_WithTheHiddenLibrariesOnlyWhenTheUserShowsThem(bool showHidden)
    {
        var userId = Guid.CreateVersion7();
        var user = User.Create("user@example.com", "auth-id");
        user.UpdateShowHiddenLibraries(showHidden);
        _userRepository.GetByIdAsync(userId).Returns(user);
        IReadOnlyList<BookInProgressDto> books =
            [new BookInProgressDto(Guid.CreateVersion7(), Guid.CreateVersion7(), "Blacksad", "Amarillo", 5, "", 12, 56, DateTime.UtcNow)];
        _bookReadService.ListInProgressAsync(userId, showHidden, Arg.Any<CancellationToken>()).Returns(books);

        var result = await _handler.Handle(new GetBooksInProgressQuery(userId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(books);
    }
}
