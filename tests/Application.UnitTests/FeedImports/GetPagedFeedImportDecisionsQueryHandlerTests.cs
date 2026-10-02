using Application.FeedImports.List;
using Application.Interfaces;
using Domain.FeedImports;
using NSubstitute;

namespace Application.UnitTests.FeedImports;

public class GetPagedFeedImportDecisionsQueryHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IFeedImportDecisionReadService _readService;
    private readonly GetPagedFeedImportDecisionsQueryHandler _handler;

    public GetPagedFeedImportDecisionsQueryHandlerTests()
    {
        _readService = Substitute.For<IFeedImportDecisionReadService>();
        _handler = new GetPagedFeedImportDecisionsQueryHandler(_readService);
    }

    public static TheoryData<GetPagedFeedImportDecisionsQuery> InvalidQueries => new()
    {
        new GetPagedFeedImportDecisionsQuery(Guid.Empty, null, null, 1, 20),
        new GetPagedFeedImportDecisionsQuery(s_userId, null, null, 0, 20),
        new GetPagedFeedImportDecisionsQuery(s_userId, null, null, 1, 0),
        new GetPagedFeedImportDecisionsQuery(s_userId, null, null, 1, GetPagedFeedImportDecisionsQueryHandler.MaxPageSize + 1),
        new GetPagedFeedImportDecisionsQuery(s_userId, (FeedImportDecisionStatus)999, null, 1, 20)
    };

    [Theory]
    [MemberData(nameof(InvalidQueries))]
    public async Task Handle_Should_ReturnBadRequest_WhenQueryIsInvalid(GetPagedFeedImportDecisionsQuery query)
    {
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedImportError.BadRequest);
        await _readService.DidNotReceive().GetPagedAsync(
            Arg.Any<Guid>(), Arg.Any<FeedImportDecisionStatus?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ForwardTrimmedFilters_WhenQueryIsValid()
    {
        var pagedList = Substitute.For<IPagedList<FeedImportDecision>>();
        _readService.GetPagedAsync(s_userId, FeedImportDecisionStatus.Pending, "blacksad", 2, 20, Arg.Any<CancellationToken>())
            .Returns(pagedList);

        var result = await _handler.Handle(
            new GetPagedFeedImportDecisionsQuery(s_userId, FeedImportDecisionStatus.Pending, "  blacksad ", 2, 20),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(pagedList);
    }
}
