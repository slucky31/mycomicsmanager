using Application.Series;
using Application.Series.Detail;
using Application.Series.Missing;
using AwesomeAssertions;
using Bunit;
using Domain.Libraries;
using Domain.Primitives;
using Domain.Series;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages.Series;
using Web.Models;
using Web.Services;
using Web.Tests.Models;
using Xunit;

namespace Web.Tests.Components.Pages.Series;

public sealed class SeriesPagesComponentTests : IAsyncDisposable
{
    private static readonly Guid s_libraryId = SeriesTestData.LibraryId;

    private readonly BunitContext _ctx = new();
    private readonly ISeriesService _seriesService = Substitute.For<ISeriesService>();
    private readonly ILibrariesService _librariesService = Substitute.For<ILibrariesService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly IBookMoveWorkflow _moveWorkflow = Substitute.For<IBookMoveWorkflow>();

    public SeriesPagesComponentTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_seriesService);
        _ctx.Services.AddSingleton(_librariesService);
        _ctx.Services.AddSingleton(_snackbar);
        _ctx.Services.AddScoped<PopupAnchorLocator>();
        _ctx.Services.AddSingleton(Substitute.For<IBooksService>());
        _ctx.Services.AddSingleton(_moveWorkflow);
        _librariesService.GetById(s_libraryId.ToString())
            .Returns(Library.Create("BD", "#594AE2", "Book", LibraryBookType.Physical, Guid.CreateVersion7()).Value!);
    }

    public async ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        await _ctx.DisposeAsync();
    }

    [Fact]
    public void LibrarySeriesPage_Should_ShowACardPerSeries_AndFilterTheIncompleteOnes()
    {
        _seriesService.GetLibrarySeries(s_libraryId, Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<SerieSummary>>.Success(
        [
            SeriesTestData.Summary("Blacksad", [1, 2], total: 2),
            SeriesTestData.Summary("Thorgal", [1, 3]),
        ]));

        var cut = _ctx.Render<LibrarySeriesPage>(p => p.Add(c => c.LibraryId, s_libraryId));

        cut.WaitForAssertion(() => cut.FindAll(".serie-card").Should().HaveCount(2));
        cut.Find(".page-bar-subtitle").TextContent.Should().Be("2 series · Series");
        cut.FindAll(".filter-chip").Select(c => c.TextContent).Should().Equal("All · 2", "Incomplete · 1", "Complete · 1");

        cut.FindAll(".filter-chip")[1].Click();

        cut.FindAll(".serie-name").Select(n => n.TextContent).Should().Equal("Thorgal");
        cut.Find(".serie-badge-missing").TextContent.Should().Be("1 missing");
    }

    [Fact]
    public void LibrarySeriesPage_Should_ReportTheError_WhenTheSeriesCannotBeLoaded()
    {
        _seriesService.GetLibrarySeries(s_libraryId, Arg.Any<CancellationToken>()).Returns(LibrariesError.NotFound);

        var cut = _ctx.Render<LibrarySeriesPage>(p => p.Add(c => c.LibraryId, s_libraryId));

        cut.WaitForAssertion(() => cut.Find(".series-empty"));
        _snackbar.Received(1).Add("Failed to load the series", Severity.Error);
    }

    [Fact]
    public void LibrarySeriesPage_Should_GoBackToTheLibraries()
    {
        _seriesService.GetLibrarySeries(s_libraryId, Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<SerieSummary>>.Success([]));

        var cut = _ctx.Render<LibrarySeriesPage>(p => p.Add(c => c.LibraryId, s_libraryId));

        cut.Find("a[aria-label='Back to libraries']").GetAttribute("href").Should().Be("/libraries/list");
    }

    [Fact]
    public async Task ChooseViewAsync_Should_SaveTheView_AndOpenTheBooks()
    {
        _seriesService.GetLibrarySeries(s_libraryId, Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<SerieSummary>>.Success([]));
        _librariesService.Update(Arg.Any<UpdateLibraryRequest>(), Arg.Any<CancellationToken>())
            .Returns(Library.Create("BD", "#594AE2", "Book", LibraryBookType.Physical, Guid.CreateVersion7()).Value!);
        var cut = _ctx.Render<LibrarySeriesPage>(p => p.Add(c => c.LibraryId, s_libraryId));
        cut.WaitForAssertion(() => cut.Find("button[aria-label='View']").HasAttribute("disabled").Should().BeFalse());

        await cut.Find("button[aria-label='View']").ClickAsync(new());
        await cut.Find("[role=dialog] .action-sheet-item:contains('List')").ClickAsync(new());

        await _librariesService.Received(1).Update(
            Arg.Is<UpdateLibraryRequest>(r => r.Id == s_libraryId.ToString() && r.DefaultView == LibraryView.List), Arg.Any<CancellationToken>());
        _ctx.Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith($"/libraries/{s_libraryId}");
    }

    [Fact]
    public async Task ChooseViewAsync_Should_StayOnTheSeries_WhenTheViewCannotBeSaved()
    {
        _seriesService.GetLibrarySeries(s_libraryId, Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<SerieSummary>>.Success([]));
        _librariesService.Update(Arg.Any<UpdateLibraryRequest>(), Arg.Any<CancellationToken>()).Returns(Result<Library>.Failure(LibrariesError.NotFound));
        var navigation = _ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"/libraries/{s_libraryId}/series");
        var cut = _ctx.Render<LibrarySeriesPage>(p => p.Add(c => c.LibraryId, s_libraryId));
        cut.WaitForAssertion(() => cut.Find("button[aria-label='View']").HasAttribute("disabled").Should().BeFalse());

        await cut.Find("button[aria-label='View']").ClickAsync(new());
        await cut.Find("[role=dialog] .action-sheet-item:contains('Covers')").ClickAsync(new());

        _snackbar.Received(1).Add("Failed to save the view", Severity.Error);
        navigation.Uri.Should().EndWith($"/libraries/{s_libraryId}/series");
    }

    [Fact]
    public void SeriePage_Should_ShowTheVolumes_AndSaveTheNumberPublished()
    {
        var books = new[] { SeriesTestData.Book("Thorgal", 1), SeriesTestData.Book("Thorgal", 3) };
        _seriesService.GetSerie(s_libraryId, "Thorgal", Arg.Any<CancellationToken>())
            .Returns(new SerieDetail("BD", SeriesTestData.Summary("Thorgal", [1, 3]), books));
        _seriesService.SetTotal(s_libraryId, "Thorgal", 5, true, Arg.Any<CancellationToken>()).Returns(Result.Success());
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/libraries/{s_libraryId}/serie?name=Thorgal");

        var cut = _ctx.Render<SeriePage>(p => p.Add(c => c.LibraryId, s_libraryId));

        cut.WaitForAssertion(() => cut.FindAll(".serie-slot, .cover-item").Should().HaveCount(3));
        cut.Find(".serie-slot-missing-number").TextContent.Should().Be("#2");
        cut.Find(".page-bar h1").GetAttribute("title").Should().Be("Thorgal");
        cut.FindAll("a.cover-link").Select(l => (l.GetAttribute("title"), l.QuerySelector(".cover-title")!.TextContent))
            .Should().Equal(("Thorgal 1", "#1 · Thorgal 1"), ("Thorgal 3", "#3 · Thorgal 3"));
        cut.Find(".serie-progress-label").TextContent.Should().Be("1 volume missing");

        cut.Find(".serie-progress-edit").Click();
        cut.Find(".serie-total-field input").Change("5");
        cut.Find(".serie-total-finished input").Change(true);
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => _seriesService.Received(1).SetTotal(s_libraryId, "Thorgal", 5, true, Arg.Any<CancellationToken>()));
        cut.WaitForAssertion(() => cut.FindAll(".serie-total-field").Should().BeEmpty());
    }

    [Fact]
    public void SeriePage_Should_ReportTheError_WhenSavingFails()
    {
        _seriesService.GetSerie(s_libraryId, "Thorgal", Arg.Any<CancellationToken>())
            .Returns(new SerieDetail("BD", SeriesTestData.Summary("Thorgal", [1]), [SeriesTestData.Book("Thorgal", 1)]));
        _seriesService.SetTotal(s_libraryId, "Thorgal", Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(SeriesError.InvalidTotalVolumes);
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/libraries/{s_libraryId}/serie?name=Thorgal");

        var cut = _ctx.Render<SeriePage>(p => p.Add(c => c.LibraryId, s_libraryId));
        cut.WaitForAssertion(() => cut.Find(".serie-progress-edit"));
        cut.Find(".serie-progress-edit").Click();
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => _snackbar.Received(1).Add(SeriesError.InvalidTotalVolumes.Description!, Severity.Error));
        cut.FindAll(".serie-total-field").Should().HaveCount(1);
    }

    [Fact]
    public void SeriePage_Should_GoBackToTheSeries_WhenTheSeriesIsNotFound()
    {
        _seriesService.GetSerie(s_libraryId, "XIII", Arg.Any<CancellationToken>()).Returns(SeriesError.NotFound);
        var navigation = _ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"/libraries/{s_libraryId}/serie?name=XIII");

        _ctx.Render<SeriePage>(p => p.Add(c => c.LibraryId, s_libraryId));

        navigation.Uri.Should().EndWith($"/libraries/{s_libraryId}/series");
        _snackbar.Received(1).Add("Series not found", Severity.Error);
    }

    [Fact]
    public void SeriePage_Should_OfferToRetryInPlace_WhenTheSeriesCannotBeLoaded()
    {
        _seriesService.GetSerie(s_libraryId, "Thorgal", Arg.Any<CancellationToken>()).Returns(SeriesError.BadRequest);
        var navigation = _ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"/libraries/{s_libraryId}/serie?name=Thorgal");

        var cut = _ctx.Render<SeriePage>(p => p.Add(c => c.LibraryId, s_libraryId));

        cut.WaitForAssertion(() => cut.Find("[role=alert]").TextContent.Should().Contain(SeriePage.Msg_LoadFailed));
        navigation.Uri.Should().EndWith($"/libraries/{s_libraryId}/serie?name=Thorgal");
        _snackbar.Received(1).Add(SeriePage.Msg_LoadFailed, Severity.Error);
        _snackbar.DidNotReceive().Add("Series not found", Arg.Any<Severity>(), Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task RetryAsync_Should_ShowTheVolumes_WhenTheSeriesCanBeLoadedAgain()
    {
        _seriesService.GetSerie(s_libraryId, "Thorgal", Arg.Any<CancellationToken>()).Returns(
            Result<SerieDetail>.Failure(SeriesError.BadRequest),
            Result<SerieDetail>.Success(new SerieDetail("BD", SeriesTestData.Summary("Thorgal", [1]), [SeriesTestData.Book("Thorgal", 1)])));
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/libraries/{s_libraryId}/serie?name=Thorgal");
        var cut = _ctx.Render<SeriePage>(p => p.Add(c => c.LibraryId, s_libraryId));
        cut.WaitForAssertion(() => cut.Find("[role=alert]"));

        await cut.Find("[role=alert] button").ClickAsync(new());

        cut.WaitForAssertion(() => cut.FindAll(".serie-slot, .cover-item").Should().HaveCount(1));
        cut.FindAll("[role=alert]").Should().BeEmpty();
    }

    // ── Actions of a book (#1220) ──────────────────────────────────────

    private IRenderedComponent<SeriePage> RenderThorgal(LibraryBookType bookType, params SerieDetail[] next)
    {
        var books = new[] { SeriesTestData.Book("Thorgal", 1), SeriesTestData.Book("Thorgal", 3) };
        var first = new SerieDetail("BD", SeriesTestData.Summary("Thorgal", [1, 3]), books, bookType);
        _seriesService.GetSerie(s_libraryId, "Thorgal", Arg.Any<CancellationToken>())
            .Returns(Result<SerieDetail>.Success(first), next.Select(d => Result<SerieDetail>.Success(d)).ToArray());
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/libraries/{s_libraryId}/serie?name=Thorgal");

        var cut = _ctx.Render<SeriePage>(p => p.Add(c => c.LibraryId, s_libraryId));
        cut.WaitForAssertion(() => cut.FindAll(".serie-slot, .cover-item").Should().HaveCount(3));
        return cut;
    }

    private static IEnumerable<string> Actions(IRenderedComponent<SeriePage> cut) =>
        cut.FindAll("[role=dialog] .action-sheet-item").Select(i => i.TextContent.Trim());

    [Fact]
    public async Task OpenBookActionsAsync_Should_OpenTheActionsOfAnOwnedBook_WithoutTheSelection()
    {
        var cut = RenderThorgal(LibraryBookType.Physical);

        cut.FindAll(".cover-actions").Select(b => b.GetAttribute("aria-label"))
            .Should().Equal("More actions for Thorgal 1", "More actions for Thorgal 3");
        await cut.FindAll(".cover-actions")[1].ClickAsync(new());

        cut.Find("[role=dialog]").GetAttribute("aria-label").Should().Be("Actions for Thorgal 3");
        cut.Find(".book-actions-serie").TextContent.Should().Be("Thorgal · #3");
        Actions(cut).Should().Equal("Edit", "Import info from the web", "Move to another library", "Delete");
    }

    [Fact]
    public async Task OpenBookActionsAt_Should_OpenTheActionsOfTheLongPressedBook_WithTheDigitalOnes()
    {
        var cut = RenderThorgal(LibraryBookType.Digital);
        var bookId = cut.FindAll(".cover-item")[0].GetAttribute("data-book-id")!;

        await cut.InvokeAsync(() => cut.Instance.OpenBookActionsAt(Guid.CreateVersion7().ToString(), PopupAnchor.At(1, 1, 1024, 768)));
        cut.FindAll("[role=dialog]").Should().BeEmpty();

        await cut.InvokeAsync(() => cut.Instance.OpenBookActionsAt(bookId, PopupAnchor.At(120, 200, 1024, 768)));
        Actions(cut).Should().Equal("Read", "Edit", "Import info from the web", "Find ISBN", "Download CBZ", "Move to another library", "Delete");
    }

    [Fact]
    public async Task MoveAsync_Should_ShowTheMovedVolumeAsMissing()
    {
        var left = new SerieDetail("BD", SeriesTestData.Summary("Thorgal", [3]), [SeriesTestData.Book("Thorgal", 3)]);
        var cut = RenderThorgal(LibraryBookType.Physical, left);
        _moveWorkflow.ChooseAndMoveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        await cut.FindAll(".cover-actions")[0].ClickAsync(new());

        await cut.Find("[role=dialog] .action-sheet-item:contains('Move to another library')").ClickAsync(new());

        cut.WaitForAssertion(() => cut.FindAll(".serie-slot-missing-number").Select(m => m.TextContent).Should().Equal("#1–2"));
        cut.FindAll("[role=dialog]").Should().BeEmpty();
    }

    [Fact]
    public async Task MoveAsync_Should_GoBackToTheSeries_WhenTheLastBookOfTheSeriesIsGone()
    {
        var cut = RenderThorgal(LibraryBookType.Physical);
        _seriesService.GetSerie(s_libraryId, "Thorgal", Arg.Any<CancellationToken>()).Returns(SeriesError.NotFound);
        _moveWorkflow.ChooseAndMoveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        await cut.FindAll(".cover-actions")[0].ClickAsync(new());

        await cut.Find("[role=dialog] .action-sheet-item:contains('Move to another library')").ClickAsync(new());

        _ctx.Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith($"/libraries/{s_libraryId}/series");
        _snackbar.DidNotReceive().Add(Arg.Any<string>(), Arg.Any<Severity>(), Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }

    [Fact]
    public void MissingVolumesPage_Should_ListTheMissingVolumes_AndFilterByLibrary()
    {
        var comics = Guid.CreateVersion7();
        _seriesService.GetMissingVolumes(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<LibraryMissingVolumes>>.Success(
        [
            new LibraryMissingVolumes(s_libraryId, "BD", [SeriesTestData.Summary("Thorgal", [1, 3])]),
            new LibraryMissingVolumes(comics, "Comics", [SeriesTestData.Summary("Saga", [2])]),
        ]));

        var cut = _ctx.Render<MissingVolumesPage>();

        cut.WaitForAssertion(() => cut.FindAll(".missing-serie").Should().HaveCount(2));
        cut.Find(".page-bar-subtitle").TextContent.Should().Be("2 volumes in 2 series");

        cut.FindAll(".filter-chip").Single(c => c.TextContent == "Comics").Click();

        cut.FindAll(".missing-library").Select(h => h.TextContent).Should().Equal("Comics");
        cut.Find(".missing-volume").TextContent.Should().Be("#1");
    }

    [Fact]
    public void MissingVolumesPage_Should_SayNothingIsMissing_AndReportAFailure()
    {
        _seriesService.GetMissingVolumes(Arg.Any<CancellationToken>()).Returns(SeriesError.BadRequest);

        var cut = _ctx.Render<MissingVolumesPage>();

        cut.WaitForAssertion(() => cut.Find(".missing-empty"));
        _snackbar.Received(1).Add("Failed to load the missing volumes", Severity.Error);
    }
}
