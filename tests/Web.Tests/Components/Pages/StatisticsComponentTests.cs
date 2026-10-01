using Application.Interfaces;
using Application.Statistics.Get;
using AwesomeAssertions;
using Bunit;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages;

public sealed class StatisticsComponentTests
{
    private static readonly Library s_library =
        Library.Create("Comics", "#FF0000", "book", LibraryBookType.Digital, Guid.CreateVersion7()).Value!;

    private static StatisticsDto CreateStatistics(int totalBooks) => new()
    {
        TotalBooks = totalBooks,
        TotalSeries = 2,
        TotalLibraries = 1,
        ReadingsPerMonth = [new MonthlyReadingCountDto(2026, 1, 4)]
    };

    private static ILibrariesService CreateLibrariesService()
    {
        var pagedList = Substitute.For<IPagedList<Library>>();
        pagedList.Items.Returns([s_library]);
        var librariesService = Substitute.For<ILibrariesService>();
        librariesService
            .FilterBy(Arg.Any<string?>(), Arg.Any<LibrariesColumn?>(), Arg.Any<SortOrder?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<IPagedList<Library>>.Success(pagedList));
        return librariesService;
    }

    private static async Task<(BunitContext Ctx, IRenderedComponent<Statistics> Cut, ISnackbar Snackbar)> RenderAsync(
        IStatisticsService statisticsService)
    {
        var snackbar = Substitute.For<ISnackbar>();
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(statisticsService);
        ctx.Services.AddSingleton(CreateLibrariesService());
        ctx.Services.AddSingleton(snackbar);

        ctx.Render<MudPopoverProvider>();
        var cut = ctx.Render<Statistics>();
        await Task.Yield();

        return (ctx, cut, snackbar);
    }

    [Fact]
    public async Task OnInitializedAsync_Should_RenderKeyFiguresForAllLibraries_WhenLoadSucceeds()
    {
        var statisticsService = Substitute.For<IStatisticsService>();
        statisticsService.Get(Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(CreateStatistics(totalBooks: 42));

        var (ctx, cut, _) = await RenderAsync(statisticsService);
        await using var _ = ctx;

        cut.WaitForAssertion(() => cut.FindAll(".stats-card").Should().NotBeEmpty());
        cut.Markup.Should().Contain("42");
        cut.Markup.Should().Contain("Libraries");
        cut.Markup.Should().Contain("Readings per month");
        cut.Markup.Should().NotContain("No books yet");
        await statisticsService.Received(1).Get(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnInitializedAsync_Should_ShowEmptyState_WhenUserHasNoBook()
    {
        var statisticsService = Substitute.For<IStatisticsService>();
        statisticsService.Get(Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(CreateStatistics(totalBooks: 0));

        var (ctx, cut, _) = await RenderAsync(statisticsService);
        await using var _ = ctx;

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("No books yet"));
    }

    [Fact]
    public async Task OnInitializedAsync_Should_ShowSnackbarError_WhenLoadFails()
    {
        var statisticsService = Substitute.For<IStatisticsService>();
        statisticsService.Get(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<StatisticsDto>.Failure(new TError("sta:err", "boom")));

        var (ctx, cut, snackbar) = await RenderAsync(statisticsService);
        await using var _ = ctx;

        cut.WaitForAssertion(() =>
            snackbar.Received(1).Add("Failed to load statistics", Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>()));
        cut.FindAll(".stats-card").Should().BeEmpty();
    }

    [Fact]
    public async Task OnLibraryChangedAsync_Should_ReloadStatisticsForLibrary_WhenLibrarySelected()
    {
        var statisticsService = Substitute.For<IStatisticsService>();
        statisticsService.Get(Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(CreateStatistics(totalBooks: 5));

        var (ctx, cut, _) = await RenderAsync(statisticsService);
        await using var _ = ctx;
        cut.WaitForAssertion(() => cut.FindAll(".stats-card").Should().NotBeEmpty());

        var select = cut.FindComponent<MudSelect<Guid>>();
        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(s_library.Id));

        await statisticsService.Received(1).Get(s_library.Id, Arg.Any<CancellationToken>());
        cut.WaitForAssertion(() => cut.Markup.Should().NotContain(">Libraries<"));
    }
}
