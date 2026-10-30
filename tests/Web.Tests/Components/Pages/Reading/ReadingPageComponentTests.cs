using Application.Books.Reading;
using Application.Interfaces;
using AwesomeAssertions;
using Bunit;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages.Reading;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Reading;

public sealed class ReadingPageComponentTests
{
    private readonly IReadingService _readingService = Substitute.For<IReadingService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly IBookMoveWorkflow _moveWorkflow = Substitute.For<IBookMoveWorkflow>();

    private static BookInProgressDto InProgress(string title) =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), "Blacksad", title, 5, "https://example.com/cover.jpg", 33, 56, DateTime.UtcNow);

    private static BookReadingDto Reading(string title, DateTime date, bool isDigital = false) =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Thorgal", title, 1, "", date, 4, isDigital);

    private static IPagedList<BookReadingDto> Page(int totalCount, params BookReadingDto[] readings)
    {
        var page = Substitute.For<IPagedList<BookReadingDto>>();
        page.Items.Returns(readings);
        page.TotalCount.Returns(totalCount);
        return page;
    }

    // A full first page: the readings of October 2026, "Le Fils" last.
    private static BookReadingDto[] FirstPage() =>
        [.. Enumerable.Range(0, ReadingPage.ReadingsPageSize)
            .Select(i => Reading(i == 0 ? "Le Fils" : $"Tome {i}", new DateTime(2026, 10, 25 - i, 20, 0, 0, DateTimeKind.Utc)))];

    private void GivenInProgress(params BookInProgressDto[] books) =>
        _readingService.GetInProgress(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<BookInProgressDto>>.Success(books));

    private void GivenReadings(int page, IPagedList<BookReadingDto> readings) =>
        _readingService.GetRecentReadings(page, ReadingPage.ReadingsPageSize, Arg.Any<CancellationToken>()).Returns(Result<IPagedList<BookReadingDto>>.Success(readings));

    private (BunitContext Ctx, IRenderedComponent<ReadingPage> Cut) Render()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(_readingService);
        ctx.Services.AddSingleton(_snackbar);
        ctx.Services.AddScoped<PopupAnchorLocator>();
        ctx.Services.AddSingleton(Substitute.For<IBooksService>());
        ctx.Services.AddSingleton(_moveWorkflow);
        return (ctx, ctx.Render<ReadingPage>());
    }

    [Fact]
    public async Task OnInitializedAsync_Should_ShowTheBooksInProgressAndTheReadingsByMonth()
    {
        var amarillo = InProgress("Amarillo");
        GivenInProgress(amarillo);
        GivenReadings(1, Page(2,
            Reading("Le Fils", new DateTime(2026, 10, 18, 20, 0, 0, DateTimeKind.Utc)),
            Reading("La Magicienne trahie", new DateTime(2026, 9, 1, 20, 0, 0, DateTimeKind.Utc))));

        var (ctx, cut) = Render();
        await using var _ = ctx;

        cut.WaitForAssertion(() => cut.Find(".reading-bar-subtitle").TextContent.Should().Be("1 in progress · 2 readings"));
        var progress = cut.Find("a.cover-link");
        progress.GetAttribute("href").Should().Be($"/books/{amarillo.Id}/read?from=%2Freading");
        progress.GetAttribute("aria-label").Should().Be("Continue Amarillo, Page 34 of 56");
        progress.QuerySelector(".reading-progress-bar").Should().NotBeNull();
        cut.FindAll(".reading-month").Select(m => m.TextContent).Should().Equal("October 2026", "September 2026");
        cut.FindAll(".reading-recent-title").Select(t => t.TextContent).Should().Equal("Le Fils", "La Magicienne trahie");
        cut.FindAll(".reading-more").Should().BeEmpty();
    }

    private static IEnumerable<string> Actions(IRenderedComponent<ReadingPage> cut) =>
        cut.FindAll("[role=dialog] .action-sheet-item").Select(i => i.TextContent.Trim());

    [Fact]
    public async Task OpenActionsAsync_Should_OpenTheDigitalActions_OfABookInProgress()
    {
        GivenInProgress(InProgress("Amarillo"));
        GivenReadings(1, Page(0));

        var (ctx, cut) = Render();
        await using var _ = ctx;
        cut.WaitForAssertion(() => cut.FindAll(".cover-actions").Should().ContainSingle());

        cut.Find(".cover-actions").GetAttribute("aria-label").Should().Be("More actions for Amarillo");
        await cut.Find(".cover-actions").ClickAsync(new());

        cut.Find("[role=dialog]").GetAttribute("aria-label").Should().Be("Actions for Amarillo");
        Actions(cut).Should().Equal("Read", "Edit", "Import info from the web", "Find ISBN", "Download CBZ", "Move to another library", "Delete");
    }

    [Theory]
    [InlineData(false, new[] { "Edit", "Import info from the web", "Move to another library", "Delete" })]
    [InlineData(true, new[] { "Read", "Edit", "Import info from the web", "Find ISBN", "Download CBZ", "Move to another library", "Delete" })]
    public async Task OpenActionsAsync_Should_OpenTheActionsOfTheBook_OfARecentReading(bool isDigital, string[] expected)
    {
        GivenInProgress();
        var reading = Reading("Le Fils", new DateTime(2026, 10, 18, 20, 0, 0, DateTimeKind.Utc), isDigital);
        GivenReadings(1, Page(1, reading));

        var (ctx, cut) = Render();
        await using var _ = ctx;
        cut.WaitForAssertion(() => cut.FindAll(".reading-recent-actions").Should().ContainSingle());

        cut.Find(".reading-recent-actions").Id.Should().Be($"reading-actions-{reading.ReadingDateId}");
        await cut.Find(".reading-recent-actions").ClickAsync(new());

        cut.Find("[role=dialog]").GetAttribute("aria-label").Should().Be("Actions for Le Fils");
        Actions(cut).Should().Equal(expected);
    }

    [Fact]
    public async Task OnBookGone_Should_LoadTheReadingsAgain_WhenTheBookIsMoved()
    {
        GivenInProgress(InProgress("Amarillo"));
        GivenReadings(1, Page(0));
        _moveWorkflow.ChooseAndMoveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        var (ctx, cut) = Render();
        await using var _ = ctx;
        cut.WaitForAssertion(() => cut.FindAll(".cover-actions").Should().ContainSingle());
        GivenInProgress();
        await cut.Find(".cover-actions").ClickAsync(new());

        await cut.Find("[role=dialog] .action-sheet-item:contains('Move to another library')").ClickAsync(new());

        cut.WaitForAssertion(() => cut.FindAll(".cover-item").Should().BeEmpty());
        cut.FindAll("[role=dialog]").Should().BeEmpty();
    }

    [Fact]
    public async Task OnInitializedAsync_Should_SayThereIsNothingYet_WhenNoBookIsStartedOrRead()
    {
        GivenInProgress();
        GivenReadings(1, Page(0));

        var (ctx, cut) = Render();
        await using var _ = ctx;

        cut.WaitForAssertion(() => cut.FindAll(".reading-section-empty").Should().HaveCount(2));
        cut.Markup.Should().Contain("No book in progress").And.Contain("No reading yet");
    }

    [Fact]
    public async Task LoadMoreAsync_Should_AddTheNextReadings_WhenShowMoreIsClicked()
    {
        GivenInProgress();
        GivenReadings(1, Page(21, FirstPage()));
        GivenReadings(2, Page(21, Reading("La Magicienne trahie", new DateTime(2026, 9, 1, 20, 0, 0, DateTimeKind.Utc))));

        var (ctx, cut) = Render();
        await using var _ = ctx;
        cut.WaitForAssertion(() => cut.Find(".reading-more").TextContent.Should().Contain("Show 1 more reading"));

        await cut.Find(".reading-more").ClickAsync(new());

        cut.WaitForAssertion(() => cut.FindAll(".reading-recent-title").Should().HaveCount(21));
        cut.FindAll(".reading-month").Select(m => m.TextContent).Should().Equal("October 2026", "September 2026");
        cut.FindAll(".reading-recent-title")[^1].TextContent.Should().Be("La Magicienne trahie");
        cut.FindAll(".reading-more").Should().BeEmpty();
    }

    [Fact]
    public async Task LoadMoreAsync_Should_KeepTheReadingsAndShowAnError_WhenTheNextPageFails()
    {
        GivenInProgress();
        GivenReadings(1, Page(21, FirstPage()));
        _readingService.GetRecentReadings(2, ReadingPage.ReadingsPageSize, Arg.Any<CancellationToken>())
            .Returns(Result<IPagedList<BookReadingDto>>.Failure(new TError("DB500", "boom")));

        var (ctx, cut) = Render();
        await using var _ = ctx;
        cut.WaitForAssertion(() => cut.FindAll(".reading-more").Should().ContainSingle());

        await cut.Find(".reading-more").ClickAsync(new());

        cut.WaitForAssertion(() =>
            _snackbar.Received(1).Add("Failed to load more readings", Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>()));
        cut.FindAll(".reading-recent-title").Should().HaveCount(ReadingPage.ReadingsPageSize);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LoadAsync_Should_OfferToRetry_WhenALoadFails(bool inProgressFails)
    {
        var failure = new TError("DB500", "boom");
        if (inProgressFails)
        {
            _readingService.GetInProgress(Arg.Any<CancellationToken>()).Returns(
                Result<IReadOnlyList<BookInProgressDto>>.Failure(failure),
                Result<IReadOnlyList<BookInProgressDto>>.Success([]));
            GivenReadings(1, Page(0));
        }
        else
        {
            GivenInProgress();
            var noReadings = Page(0);
            _readingService.GetRecentReadings(1, ReadingPage.ReadingsPageSize, Arg.Any<CancellationToken>()).Returns(
                Result<IPagedList<BookReadingDto>>.Failure(failure),
                Result<IPagedList<BookReadingDto>>.Success(noReadings));
        }

        var (ctx, cut) = Render();
        await using var _ = ctx;
        cut.WaitForAssertion(() => cut.Find("[role=alert]").TextContent.Should().Contain("Couldn't load your readings"));
        _snackbar.Received(1).Add("Failed to load your readings", Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());

        await cut.Find("[role=alert] button").ClickAsync(new());

        cut.WaitForAssertion(() => cut.FindAll(".reading-section-empty").Should().HaveCount(2));
        cut.FindAll("[role=alert]").Should().BeEmpty();
    }
}
