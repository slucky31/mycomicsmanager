using Application.Series;
using Application.Series.Missing;
using Domain.Series;
using NSubstitute;

namespace Application.UnitTests.Series;

public class GetMissingVolumesQueryHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly ISeriesReadService _readService = Substitute.For<ISeriesReadService>();
    private readonly GetMissingVolumesQueryHandler _handler;

    public GetMissingVolumesQueryHandlerTests()
    {
        _handler = new GetMissingVolumesQueryHandler(_readService);
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
}
