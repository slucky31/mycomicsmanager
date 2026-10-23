using Application.Interfaces;
using Application.Series;
using Application.Series.Detail;
using Domain.Libraries;
using Domain.Series;
using NSubstitute;

namespace Application.UnitTests.Series;

public class GetSerieQueryHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly ISeriesReadService _readService = Substitute.For<ISeriesReadService>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly GetSerieQueryHandler _handler;
    private readonly Library _library = Library.Create("BD", "#594AE2", "Book", LibraryBookType.Physical, s_userId).Value!;

    public GetSerieQueryHandlerTests()
    {
        _handler = new GetSerieQueryHandler(_readService, _libraryRepository);
        _libraryRepository.GetByIdAsync(_library.Id).Returns(_library);
        _readService.ListBooksAsync(s_userId, _library.Id, Arg.Any<CancellationToken>())
            .Returns([SerieRows.Book(_library.Id, "Thorgal", 3), SerieRows.Book(_library.Id, "thorgal", 1), SerieRows.Book(_library.Id, "Blacksad", 1)]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Handle_Should_ReturnBadRequest_WhenTheNameIsBlank(string name)
    {
        var result = await _handler.Handle(new GetSerieQuery(s_userId, _library.Id, name), TestContext.Current.CancellationToken);

        result.Error.Should().Be(SeriesError.BadRequest);
        await _libraryRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTheLibraryDoesNotExist()
    {
        var result = await _handler.Handle(new GetSerieQuery(s_userId, Guid.CreateVersion7(), "Thorgal"), TestContext.Current.CancellationToken);

        result.Error.Should().Be(LibrariesError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnSeriesNotFound_WhenNoBookIsInTheSeries()
    {
        var result = await _handler.Handle(new GetSerieQuery(s_userId, _library.Id, "XIII"), TestContext.Current.CancellationToken);

        result.Error.Should().Be(SeriesError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheBooksOfTheSeriesByVolume_WithWhatTheUserTold()
    {
        _readService.ListInfosAsync(s_userId, _library.Id, Arg.Any<CancellationToken>())
            .Returns([SeriesInfo.Create(_library.Id, "Thorgal", 4, isFinished: false).Value!]);

        var result = await _handler.Handle(new GetSerieQuery(s_userId, _library.Id, " THORGAL "), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var detail = result.Value!;
        detail.LibraryName.Should().Be("BD");
        detail.Books.Select(b => b.VolumeNumber).Should().Equal(1, 3);
        detail.Summary.Completeness.Missing.Should().Equal(new VolumeRange(2, 2), new VolumeRange(4, 4));
    }
}
