using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Series;
using Application.Series.Detail;
using Application.Series.List;
using Application.Series.Missing;
using Application.Series.SetTotal;
using AwesomeAssertions;
using Domain.Primitives;
using Domain.Series;
using Domain.Users;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class SeriesServiceTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Guid s_libraryId = Guid.CreateVersion7();

    private readonly IQueryHandler<GetLibrarySeriesQuery, IReadOnlyList<SerieSummary>> _listHandler = Substitute.For<IQueryHandler<GetLibrarySeriesQuery, IReadOnlyList<SerieSummary>>>();
    private readonly IQueryHandler<GetSerieQuery, SerieDetail> _getHandler = Substitute.For<IQueryHandler<GetSerieQuery, SerieDetail>>();
    private readonly IQueryHandler<GetMissingVolumesQuery, IReadOnlyList<LibraryMissingVolumes>> _missingHandler = Substitute.For<IQueryHandler<GetMissingVolumesQuery, IReadOnlyList<LibraryMissingVolumes>>>();
    private readonly ICommandHandler<SetSerieTotalCommand> _setTotalHandler = Substitute.For<ICommandHandler<SetSerieTotalCommand>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly SeriesService _service;

    public SeriesServiceTests()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(s_userId);
        _service = new SeriesService(_listHandler, _getHandler, _missingHandler, _setTotalHandler, _currentUserService);
    }

    public static TheoryData<string> Methods => ["list", "get", "missing", "setTotal"];

    private async Task<ResultBase> CallAsync(string method) => method switch
    {
        "list" => await _service.GetLibrarySeries(s_libraryId, TestContext.Current.CancellationToken),
        "get" => await _service.GetSerie(s_libraryId, "Thorgal", TestContext.Current.CancellationToken),
        "missing" => await _service.GetMissingVolumes(TestContext.Current.CancellationToken),
        _ => await _service.SetTotal(s_libraryId, "Thorgal", 41, isFinished: true, TestContext.Current.CancellationToken),
    };

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task Methods_Should_ReturnTheUserError_WhenTheUserIsNotResolved(string method)
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(Result<Guid>.Failure(UsersError.NotFound));

        var result = await CallAsync(method);

        result.Error.Should().Be(UsersError.NotFound);
        await _listHandler.DidNotReceiveWithAnyArgs().Handle(default!, TestContext.Current.CancellationToken);
        await _getHandler.DidNotReceiveWithAnyArgs().Handle(default!, TestContext.Current.CancellationToken);
        await _missingHandler.DidNotReceiveWithAnyArgs().Handle(default!, TestContext.Current.CancellationToken);
        await _setTotalHandler.DidNotReceiveWithAnyArgs().Handle(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetLibrarySeries_Should_AskForTheSeriesOfTheLibraryOfTheUser()
    {
        IReadOnlyList<SerieSummary> series = [];
        _listHandler.Handle(new GetLibrarySeriesQuery(s_userId, s_libraryId), Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<SerieSummary>>.Success(series));

        var result = await _service.GetLibrarySeries(s_libraryId, TestContext.Current.CancellationToken);

        result.Value.Should().BeSameAs(series);
    }

    [Fact]
    public async Task GetSerie_Should_AskForTheSeriesByName()
    {
        _getHandler.Handle(new GetSerieQuery(s_userId, s_libraryId, "Thorgal"), Arg.Any<CancellationToken>()).Returns(SeriesError.NotFound);

        var result = await _service.GetSerie(s_libraryId, "Thorgal", TestContext.Current.CancellationToken);

        result.Error.Should().Be(SeriesError.NotFound);
    }

    [Fact]
    public async Task GetMissingVolumes_Should_AskForTheMissingVolumesOfTheUser()
    {
        IReadOnlyList<LibraryMissingVolumes> missing = [];
        _missingHandler.Handle(new GetMissingVolumesQuery(s_userId), Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<LibraryMissingVolumes>>.Success(missing));

        var result = await _service.GetMissingVolumes(TestContext.Current.CancellationToken);

        result.Value.Should().BeSameAs(missing);
    }

    [Fact]
    public async Task SetTotal_Should_SendTheTotalOfTheSeries()
    {
        _setTotalHandler.Handle(Arg.Any<SetSerieTotalCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _service.SetTotal(s_libraryId, "Thorgal", 41, isFinished: true, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _setTotalHandler.Received(1).Handle(new SetSerieTotalCommand(s_userId, s_libraryId, "Thorgal", 41, true), Arg.Any<CancellationToken>());
    }
}
