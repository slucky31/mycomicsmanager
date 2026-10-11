using Application.Books.List;
using Application.Interfaces;
using AwesomeAssertions;
using Bunit;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Web.Components.Pages.Libraries;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Libraries;

public sealed class LibraryDetailPageIsbnScanTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly Library _library = Library.Create("Comics", "#5C6BC0", "Bookmark", LibraryBookType.Digital, Guid.CreateVersion7()).Value!;
    private readonly IIsbnScanService _isbnScanService = Substitute.For<IIsbnScanService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();

    public LibraryDetailPageIsbnScanTests()
    {
        var librariesService = Substitute.For<ILibrariesService>();
        librariesService.GetById(_library.Id.ToString()).Returns(Result<Library>.Success(_library));
        var page = Substitute.For<IPagedList<BookSummaryDto>>();
        page.Items.Returns([]);
        var booksService = Substitute.For<IBooksService>();
        booksService.GetPagedByLibrary(_library.Id, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<BookSortOrder>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<IPagedList<BookSummaryDto>>.Success(page));

        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(librariesService);
        _ctx.Services.AddSingleton(booksService);
        _ctx.Services.AddSingleton(Substitute.For<IBookMoveWorkflow>());
        _ctx.Services.AddSingleton(new LibraryStateService());
        _ctx.Services.AddSingleton(_isbnScanService);
        _ctx.Services.AddSingleton(_snackbar);
    }

    public async ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        await _ctx.DisposeAsync();
    }

    private async Task FindMissingIsbnsAsync()
    {
        var cut = _ctx.Render<LibraryDetailPage>(p => p.Add(c => c.LibraryId, _library.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Comics"));
        await cut.Find("button[aria-label='Find the missing ISBNs in the pages']").ClickAsync(new());
    }

    private void ShouldShow(string message, Severity severity) =>
        _snackbar.Received(1).Add(message, severity, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());

    [Fact]
    public async Task FindMissingIsbnsAsync_Should_TellHowManyBooksAreScanned()
    {
        _isbnScanService.StartLibraryScanAsync(_library.Id, Arg.Any<CancellationToken>()).Returns(Result<int>.Success(3));

        await FindMissingIsbnsAsync();

        ShouldShow("Reading the pages of 3 book(s) in the background. A book showing several ISBNs lets you pick its own.", Severity.Success);
    }

    [Fact]
    public async Task FindMissingIsbnsAsync_Should_TellNothingIsToScan_WhenEveryBookHasAnIsbn()
    {
        _isbnScanService.StartLibraryScanAsync(_library.Id, Arg.Any<CancellationToken>()).Returns(Result<int>.Success(0));

        await FindMissingIsbnsAsync();

        ShouldShow("Every book of this library has an ISBN or was already scanned", Severity.Info);
    }

    [Fact]
    public async Task FindMissingIsbnsAsync_Should_ShowAnError_WhenTheScanCannotStart()
    {
        _isbnScanService.StartLibraryScanAsync(_library.Id, Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(LibrariesError.NotFound));

        await FindMissingIsbnsAsync();

        ShouldShow("Unable to start the search of the missing ISBNs", Severity.Error);
    }

    [Fact]
    public async Task FindMissingIsbnsAsync_Should_ShowAnError_WhenStartingTheScanThrows()
    {
        _isbnScanService.StartLibraryScanAsync(_library.Id, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException());

        await FindMissingIsbnsAsync();

        ShouldShow("Unable to start the search of the missing ISBNs", Severity.Error);
    }
}
