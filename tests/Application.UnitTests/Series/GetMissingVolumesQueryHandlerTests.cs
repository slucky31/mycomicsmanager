using Application.Interfaces;
using Application.Libraries;
using Application.Series;
using Application.Series.Missing;
using Domain.Series;
using Domain.Users;
using NSubstitute;

namespace Application.UnitTests.Series;

public class GetMissingVolumesQueryHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly ISeriesReadService _readService = Substitute.For<ISeriesReadService>();
    private readonly ILibraryReadService _libraryReadService = Substitute.For<ILibraryReadService>();
    private readonly IRepository<User, Guid> _userRepository = Substitute.For<IRepository<User, Guid>>();
    private readonly GetMissingVolumesQueryHandler _handler;

    public GetMissingVolumesQueryHandlerTests()
    {
        _handler = new GetMissingVolumesQueryHandler(_readService, _libraryReadService, _userRepository);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheUserIsEmpty()
    {
        var result = await _handler.Handle(new GetMissingVolumesQuery(Guid.Empty), TestContext.Current.CancellationToken);

        result.Error.Should().Be(SeriesError.BadRequest);
        await _readService.DidNotReceiveWithAnyArgs().ListBooksAsync(default, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_Should_ReturnOnlyTheIncompleteSeries_ByLibraryName()
    {
        var comics = Guid.CreateVersion7();
        var bd = Guid.CreateVersion7();
        _readService.ListBooksAsync(s_userId, null, Arg.Any<CancellationToken>()).Returns(
        [
            SerieRows.Book(comics, "Saga", 1, libraryName: "Comics"),
            SerieRows.Book(comics, "Saga", 3, libraryName: "Comics"),
            SerieRows.Book(bd, "Thorgal", 1, libraryName: "BD"),
            SerieRows.Book(bd, "Blacksad", 1, libraryName: "BD"),
            SerieRows.Book(bd, "Blacksad", 2, libraryName: "BD"),
            SerieRows.Book(bd, "One Shot", 4, libraryName: "BD"),
        ]);
        _readService.ListInfosAsync(s_userId, null, Arg.Any<CancellationToken>())
            .Returns([SeriesInfo.Create(bd, "Thorgal", 3, isFinished: true).Value!]);

        var result = await _handler.Handle(new GetMissingVolumesQuery(s_userId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(l => l.LibraryName).Should().Equal("BD", "Comics");
        result.Value![0].Series.Select(s => s.Name).Should().Equal("Thorgal");
        result.Value![0].MissingCount.Should().Be(2);
        result.Value![1].Series.Select(s => s.Name).Should().Equal("Saga");
    }

    [Fact]
    public async Task Handle_Should_LeaveOutTheHiddenLibraries_UnlessTheUserShowsThem()
    {
        var comics = Guid.CreateVersion7();
        var hidden = Guid.CreateVersion7();
        _readService.ListBooksAsync(s_userId, null, Arg.Any<CancellationToken>()).Returns(
        [
            SerieRows.Book(comics, "Saga", 1, libraryName: "Comics"),
            SerieRows.Book(comics, "Saga", 3, libraryName: "Comics"),
            SerieRows.Book(hidden, "Thorgal", 1, libraryName: "Loans"),
            SerieRows.Book(hidden, "Thorgal", 3, libraryName: "Loans"),
        ]);
        _readService.ListInfosAsync(s_userId, null, Arg.Any<CancellationToken>())
            .Returns([SeriesInfo.Create(hidden, "Thorgal", 4, isFinished: true).Value!]);
        _libraryReadService.ListHiddenIdsAsync(s_userId, Arg.Any<CancellationToken>()).Returns(new HashSet<Guid> { hidden });

        var withoutHidden = await _handler.Handle(new GetMissingVolumesQuery(s_userId), TestContext.Current.CancellationToken);
        var user = User.Create("user@example.com", "auth-id");
        user.UpdateShowHiddenLibraries(true);
        _userRepository.GetByIdAsync(s_userId).Returns(user);
        var withHidden = await _handler.Handle(new GetMissingVolumesQuery(s_userId), TestContext.Current.CancellationToken);

        withoutHidden.Value!.Select(l => l.LibraryName).Should().Equal("Comics");
        withHidden.Value!.Select(l => l.LibraryName).Should().Equal("Comics", "Loans");
        await _libraryReadService.Received(1).ListHiddenIdsAsync(s_userId, Arg.Any<CancellationToken>());
    }
}
