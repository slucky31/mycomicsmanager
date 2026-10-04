using Application.FeedImports;
using Application.FeedImports.Sync;
using Application.Interfaces;
using Domain.FeedImports;
using Domain.Primitives;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Application.UnitTests.FeedImports;

public class SyncFeedImportsCommandHandlerTests
{
    private const long CategoryId = 7;

    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IMinifluxClient _minifluxClient;
    private readonly IFeedImportDecisionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MinifluxSettings _settings = new() { CategoryName = "BD" };
    private readonly SyncFeedImportsCommandHandler _handler;

    public SyncFeedImportsCommandHandlerTests()
    {
        _minifluxClient = Substitute.For<IMinifluxClient>();
        _repository = Substitute.For<IFeedImportDecisionRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));

        _minifluxClient.GetCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<MinifluxCategory>>.Success([new MinifluxCategory(1, "Tech"), new MinifluxCategory(CategoryId, " bd ")]));
        _minifluxClient.UnstarAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        _handler = new SyncFeedImportsCommandHandler(_minifluxClient, _repository, _unitOfWork, Options.Create(_settings), NullLogger<SyncFeedImportsCommandHandler>.Instance);
    }

    private static MinifluxEntry CreateEntry(long id) =>
        new(id, $"Blacksad T{id}", $"https://planete-bd.org/blacksad-{id}", new DateTimeOffset(2026, 9, 30, 20, 0, 0, TimeSpan.FromHours(2)));

    private void GivenStarredEntries(params MinifluxEntry[] entries) =>
        _minifluxClient.GetStarredEntriesAsync(CategoryId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<MinifluxEntry>>.Success(entries));

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenUserIdIsEmpty()
    {
        var result = await _handler.Handle(new SyncFeedImportsCommand(Guid.Empty), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedImportError.BadRequest);
        await _minifluxClient.DidNotReceive().GetCategoriesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnCategoryNotFound_WhenCategoryDoesNotExistInMiniflux()
    {
        _settings.CategoryName = "Mangas";

        var result = await _handler.Handle(new SyncFeedImportsCommand(s_userId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedImportError.CategoryNotFound);
        await _minifluxClient.DidNotReceive().GetStarredEntriesAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnMinifluxError_WhenStarredEntriesCannotBeFetched()
    {
        _minifluxClient.GetStarredEntriesAsync(CategoryId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<MinifluxEntry>>.Failure(FeedImportError.MinifluxUnavailable));

        var result = await _handler.Handle(new SyncFeedImportsCommand(s_userId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedImportError.MinifluxUnavailable);
        _repository.DidNotReceive().Add(Arg.Any<FeedImportDecision>());
    }

    [Fact]
    public async Task Handle_Should_SaveDecisionBeforeUnstarring_WhenEntryIsNew()
    {
        var entry = CreateEntry(101);
        GivenStarredEntries(entry);

        var result = await _handler.Handle(new SyncFeedImportsCommand(s_userId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new SyncFeedImportsResult(StarredEntries: 1, Created: 1, AlreadyKnown: 0, Rejected: 0, UnstarFailures: 0));
        Received.InOrder(() =>
        {
            _repository.Add(Arg.Is<FeedImportDecision>(d =>
                d.UserId == s_userId &&
                d.MinifluxEntryId == entry.Id &&
                d.EntryTitle == entry.Title &&
                d.EntryUrl == entry.Url &&
                d.PublishedAt == entry.PublishedAt!.Value.UtcDateTime &&
                d.Status == FeedImportDecisionStatus.Pending));
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _minifluxClient.UnstarAsync(entry.Id, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_Should_OnlyRetryUnstar_WhenEntryIsAlreadyKnown()
    {
        var entry = CreateEntry(102);
        GivenStarredEntries(entry);
        var existing = FeedImportDecision.Create(s_userId, entry.Id, entry.Title, entry.Url, null).Value!;
        _repository.GetByMinifluxEntryIdAsync(s_userId, entry.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.Handle(new SyncFeedImportsCommand(s_userId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AlreadyKnown.Should().Be(1);
        result.Value.Created.Should().Be(0);
        _repository.DidNotReceive().Add(Arg.Any<FeedImportDecision>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _minifluxClient.Received(1).UnstarAsync(entry.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_KeepDecisionAndContinue_WhenUnstarFails()
    {
        var failing = CreateEntry(103);
        var next = CreateEntry(104);
        GivenStarredEntries(failing, next);
        _minifluxClient.UnstarAsync(failing.Id, Arg.Any<CancellationToken>()).Returns(Result.Failure(FeedImportError.MinifluxUnavailable));

        var result = await _handler.Handle(new SyncFeedImportsCommand(s_userId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new SyncFeedImportsResult(StarredEntries: 2, Created: 2, AlreadyKnown: 0, Rejected: 0, UnstarFailures: 1));
        _repository.DidNotReceive().Remove(Arg.Any<FeedImportDecision>());
        await _minifluxClient.Received(1).UnstarAsync(next.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotUnstarAndDetachDecision_WhenSaveFails()
    {
        var entry = CreateEntry(105);
        GivenStarredEntries(entry);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(new TError("DB_UPDATE_ERROR", "duplicate key")));

        var result = await _handler.Handle(new SyncFeedImportsCommand(s_userId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Rejected.Should().Be(1);
        result.Value.UnstarFailures.Should().Be(0);
        _repository.Received(1).Remove(Arg.Is<FeedImportDecision>(d => d.MinifluxEntryId == entry.Id));
        await _minifluxClient.DidNotReceive().UnstarAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotSaveNorUnstar_WhenEntryUrlIsInvalid()
    {
        var invalid = new MinifluxEntry(106, "Sans lien", "not-an-url", null);
        GivenStarredEntries(invalid);

        var result = await _handler.Handle(new SyncFeedImportsCommand(s_userId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Rejected.Should().Be(1);
        _repository.DidNotReceive().Add(Arg.Any<FeedImportDecision>());
        await _minifluxClient.DidNotReceive().UnstarAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }
}
