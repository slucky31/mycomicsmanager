using Application.Books.Read;
using AwesomeAssertions;
using Bunit;
using Domain.Books;
using Domain.Errors;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages.Books;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Books;

public sealed class BookReaderComponentTests : IAsyncDisposable
{
    private readonly Guid _bookId = Guid.CreateVersion7();
    private readonly BunitContext _ctx = new();
    private readonly IBookReaderService _readerService = Substitute.For<IBookReaderService>();
    private readonly IBooksService _booksService = Substitute.For<IBooksService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();

    public BookReaderComponentTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./js/bookReader.js");
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_readerService);
        _ctx.Services.AddSingleton(_booksService);
        _ctx.Services.AddSingleton(_snackbar);
        _readerService.SaveProgressAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    public async ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        await _ctx.DisposeAsync();
    }

    private IRenderedComponent<BookReader> RenderReader(int pageCount, int lastReadPage)
    {
        _readerService.GetReaderInfoAsync(_bookId, Arg.Any<CancellationToken>())
            .Returns(new BookReaderInfoDto(_bookId, "Blacksad", "Arctic Nation", 2, pageCount, lastReadPage));
        return _ctx.Render<BookReader>(p => p.Add(c => c.BookId, _bookId.ToString()));
    }

    private static string CurrentPageSource(IRenderedComponent<BookReader> cut) =>
        cut.Find("img.reader-page").GetAttribute("src")!;

    [Fact]
    public async Task OnInitializedAsync_Should_OpenTheLastReadPage()
    {
        var cut = RenderReader(pageCount: 20, lastReadPage: 7);

        await cut.WaitForAssertionAsync(() => CurrentPageSource(cut).Should().Be($"/api/books/{_bookId}/pages/7"));
        cut.Markup.Should().Contain("8 / 20");
    }

    [Fact]
    public async Task NextPageAsync_Should_ShowTheFollowingPage()
    {
        var cut = RenderReader(pageCount: 20, lastReadPage: 7);
        await cut.WaitForAssertionAsync(() => cut.FindAll("img.reader-page").Should().ContainSingle());

        await cut.Find("button.reader-zone-next").ClickAsync(new());

        CurrentPageSource(cut).Should().Be($"/api/books/{_bookId}/pages/8");
    }

    [Fact]
    public async Task CloseAsync_Should_SaveTheCurrentPage()
    {
        var cut = RenderReader(pageCount: 20, lastReadPage: 7);
        await cut.WaitForAssertionAsync(() => cut.FindAll("img.reader-page").Should().ContainSingle());
        await cut.Find("button.reader-zone-previous").ClickAsync(new());

        await cut.Find("button[aria-label='Close reader']").ClickAsync(new());

        await _readerService.Received(1).SaveProgressAsync(_bookId, 6, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsReadAsync_Should_AddReadingDateAndRestartTheBook_WhenLastPageIsPassed()
    {
        _booksService.AddReadingDate(_bookId.ToString(), 3, Arg.Any<CancellationToken>()).Returns(Result<ReadingDate>.Success(null!));
        var cut = RenderReader(pageCount: 3, lastReadPage: 2);
        await cut.WaitForAssertionAsync(() => cut.FindAll("img.reader-page").Should().ContainSingle());

        await cut.Find("button.reader-zone-next").ClickAsync(new());
        cut.Markup.Should().Contain("You finished this book");

        await cut.FindAll("button").Single(b => b.TextContent.Contains("Mark as read", StringComparison.Ordinal)).ClickAsync(new());

        await _booksService.Received(1).AddReadingDate(_bookId.ToString(), 3, Arg.Any<CancellationToken>());
        await _readerService.Received(1).SaveProgressAsync(_bookId, 0, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnInitializedAsync_Should_ShowAnError_WhenTheBookCannotBeOpened()
    {
        _readerService.GetReaderInfoAsync(_bookId, Arg.Any<CancellationToken>())
            .Returns(Result<BookReaderInfoDto>.Failure(FileProcessingError.CorruptArchive));

        var cut = _ctx.Render<BookReader>(p => p.Add(c => c.BookId, _bookId.ToString()));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("This book cannot be read"));
        _snackbar.Received(1).Add(FileProcessingError.CorruptArchive.Description!, Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }
}
