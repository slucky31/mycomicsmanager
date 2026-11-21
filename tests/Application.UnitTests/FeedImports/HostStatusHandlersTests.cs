using Application.FeedImports;
using Application.FeedImports.HostStatus;
using Application.Interfaces;
using Domain.FeedImports;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.FeedImports;

public class HostStatusHandlersTests
{
    private static readonly DateTime s_now = new(2026, 11, 21, 23, 0, 0, DateTimeKind.Utc);

    private readonly IDebridLinkClient _debridLink = Substitute.For<IDebridLinkClient>();
    private readonly IHostStatusChangeRepository _repository = Substitute.For<IHostStatusChangeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FixedTimeProvider _time = new(s_now);
    private readonly RecordHostStatusesCommandHandler _recordHandler;
    private readonly GetHostStatusOverviewQueryHandler _overviewHandler;

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public DateTime UtcNow { get; set; } = utcNow;
        public override DateTimeOffset GetUtcNow() => new(UtcNow);
    }

    public HostStatusHandlersTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success<int>(1));
        _repository.GetLatestPerHostAsync(Arg.Any<CancellationToken>()).Returns(new Dictionary<string, HostStatusChange>());
        _repository.ListSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([]);
        _recordHandler = new RecordHostStatusesCommandHandler(_debridLink, _repository, _unitOfWork, _time);
        _overviewHandler = new GetHostStatusOverviewQueryHandler(_debridLink, _repository, _time);
    }

    private void GivenHosts(params DebridLinkHost[] hosts) =>
        _debridLink.GetHostsAsync(Arg.Any<CancellationToken>()).Returns(Result.Success<IReadOnlyList<DebridLinkHost>>(hosts));

    private HostStatusChange Change(string host, bool isUp, DateTime at)
    {
        _time.UtcNow = at;
        var change = HostStatusChange.Create(host, isUp, _time).Value!;
        _time.UtcNow = s_now;
        return change;
    }

    private void GivenLatest(params HostStatusChange[] changes) =>
        _repository.GetLatestPerHostAsync(Arg.Any<CancellationToken>()).Returns(changes.ToDictionary(c => c.Host));

    [Fact]
    public async Task RecordHandle_Should_AddOnlyTheChanges_AndReportTheHostsBackUp()
    {
        GivenHosts(
            new DebridLinkHost("ddl", true, ["ddl.to"]),
            new DebridLinkHost("1fichier", true, ["1fichier.com"]),
            new DebridLinkHost("elitefile", false, ["elitefile.net"]));
        GivenLatest(Change("ddl", false, s_now.AddHours(-2)), Change("1fichier", true, s_now.AddDays(-1)));

        var result = await _recordHandler.Handle(new RecordHostStatusesCommand(), TestContext.Current.CancellationToken);

        result.Value!.Changes.Should().Be(2);
        result.Value.CameBackUp.Should().Equal("ddl");
        _repository.Received(1).Add(Arg.Is<HostStatusChange>(c => c.Host == "ddl" && c.IsUp && c.ChangedAt == s_now));
        _repository.Received(1).Add(Arg.Is<HostStatusChange>(c => c.Host == "elitefile" && !c.IsUp));
        await _repository.Received(1).DeleteOlderThanAsync(s_now - FeedImportConstants.HostStatusRetention, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordHandle_Should_NotSave_WhenNothingChanged()
    {
        GivenHosts(new DebridLinkHost("1fichier", true, ["1fichier.com"]));
        GivenLatest(Change("1fichier", true, s_now.AddDays(-1)));

        var result = await _recordHandler.Handle(new RecordHostStatusesCommand(), TestContext.Current.CancellationToken);

        result.Value!.Changes.Should().Be(0);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordHandle_Should_ReturnTheError_WhenDebridLinkIsUnreachable()
    {
        _debridLink.GetHostsAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<DebridLinkHost>>(FeedImportError.DebridLinkUnavailable));

        var result = await _recordHandler.Handle(new RecordHostStatusesCommand(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(FeedImportError.DebridLinkUnavailable);
        _repository.DidNotReceive().Add(Arg.Any<HostStatusChange>());
    }

    [Fact]
    public async Task RecordHandle_Should_ReturnTheSaveError_WhenSavingFails()
    {
        GivenHosts(new DebridLinkHost("ddl", false, ["ddl.to"]));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure<int>(new TError("DB", "down")));

        var result = await _recordHandler.Handle(new RecordHostStatusesCommand(), TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("DB");
    }

    [Fact]
    public async Task OverviewHandle_Should_ListTheHostsDownFirst_WithSinceAndChanges()
    {
        GivenHosts(new DebridLinkHost("1fichier", true, ["1fichier.com"]), new DebridLinkHost("ddl", false, ["ddl.to"]));
        var firstDdl = Change("ddl", true, s_now.AddDays(-10));
        var downDdl = Change("ddl", false, s_now.AddHours(-5));
        var firstFichier = Change("1fichier", true, s_now.AddDays(-3));
        _repository.ListSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([downDdl, firstFichier, firstDdl]);
        GivenLatest(downDdl, firstFichier);

        var result = await _overviewHandler.Handle(new GetHostStatusOverviewQuery(), TestContext.Current.CancellationToken);

        var overview = result.Value!;
        overview.IsLive.Should().BeTrue();
        overview.Hosts.Select(h => h.Name).Should().Equal("ddl", "1fichier");
        overview.Hosts[0].Should().BeEquivalentTo(new HostStatusRow("ddl", false, ["ddl.to"], downDdl.ChangedAt, false, 1));
        overview.Hosts[1].Should().BeEquivalentTo(new HostStatusRow("1fichier", true, ["1fichier.com"], firstFichier.ChangedAt, true, 0));
        overview.FindByDomain("www.ddl.to")!.Name.Should().Be("ddl");
        overview.FindByDomain("turbobit.net").Should().BeNull();
    }

    [Fact]
    public async Task OverviewHandle_Should_ShowTheLatestRecordedStates_WhenDebridLinkIsUnreachable()
    {
        _debridLink.GetHostsAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<DebridLinkHost>>(FeedImportError.DebridLinkUnavailable));
        var down = Change("ddl", false, s_now.AddHours(-1));
        GivenLatest(down);

        var result = await _overviewHandler.Handle(new GetHostStatusOverviewQuery(), TestContext.Current.CancellationToken);

        result.Value!.IsLive.Should().BeFalse();
        result.Value.Hosts.Should().ContainSingle().Which.IsUp.Should().BeFalse();
    }

    [Fact]
    public async Task OverviewHandle_Should_LeaveSinceEmpty_WhenTheStateIsNotRecordedYet()
    {
        GivenHosts(new DebridLinkHost("ddl", true, ["ddl.to"]));
        GivenLatest(Change("ddl", false, s_now.AddHours(-1)));

        var result = await _overviewHandler.Handle(new GetHostStatusOverviewQuery(), TestContext.Current.CancellationToken);

        result.Value!.Hosts.Should().ContainSingle().Which.Since.Should().BeNull();
    }
}
