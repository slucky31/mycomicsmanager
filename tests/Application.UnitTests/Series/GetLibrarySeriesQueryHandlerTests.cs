using Application.Interfaces;
using Application.Series;
using Application.Series.List;
using Domain.Libraries;
using Domain.Series;
using NSubstitute;

namespace Application.UnitTests.Series;

public class GetLibrarySeriesQueryHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly ISeriesReadService _readService = Substitute.For<ISeriesReadService>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly GetLibrarySeriesQueryHandler _handler;
    private readonly Library _library = Library.Create("BD", "#594AE2", "Book", LibraryBookType.Physical, s_userId).Value!;

    public GetLibrarySeriesQueryHandlerTests()
    {
        _handler = new GetLibrarySeriesQueryHandler(_readService, _libraryRepository);
        _libraryRepository.GetByIdAsync(_library.Id).Returns(_library);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheLibraryIsEmpty()
    {
        var result = await _handler.Handle(new GetLibrarySeriesQuery(s_userId, Guid.Empty), TestContext.Current.CancellationToken);

        result.Error.Should().Be(SeriesError.BadRequest);
        await _libraryRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTheLibraryIsAnotherUsers()
    {
        var result = await _handler.Handle(new GetLibrarySeriesQuery(Guid.CreateVersion7(), _library.Id), TestContext.Current.CancellationToken);

        result.Error.Should().Be(LibrariesError.NotFound);
        await _readService.DidNotReceiveWithAnyArgs().ListBooksAsync(default, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheSeriesOfTheLibrary()
    {
        _readService.ListBooksAsync(s_userId, _library.Id, Arg.Any<CancellationToken>())
            .Returns([SerieRows.Book(_library.Id, "Thorgal", 1), SerieRows.Book(_library.Id, "Thorgal", 3)]);
        _readService.ListInfosAsync(s_userId, _library.Id, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetLibrarySeriesQuery(s_userId, _library.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var serie = result.Value!.Single();
        serie.Name.Should().Be("Thorgal");
        serie.Completeness.Missing.Should().Equal(new VolumeRange(2, 2));
    }
}
