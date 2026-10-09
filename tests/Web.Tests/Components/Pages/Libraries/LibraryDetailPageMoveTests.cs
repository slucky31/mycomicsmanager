using Application.Books.List;
using Application.Interfaces;
using AwesomeAssertions;
using Bunit;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Web.Components.Pages.Libraries;
using Web.Enums;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Libraries;

public sealed class LibraryDetailPageMoveTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly Library _library = Library.Create("Comics", "#5C6BC0", "Bookmark", LibraryBookType.Digital, Guid.CreateVersion7()).Value!;
    private readonly IBooksService _booksService = Substitute.For<IBooksService>();
    private readonly IBookMoveWorkflow _workflow = Substitute.For<IBookMoveWorkflow>();
    private readonly LibraryStateService _stateService = new();

    public LibraryDetailPageMoveTests()
    {
        var librariesService = Substitute.For<ILibrariesService>();
        librariesService.GetById(_library.Id.ToString()).Returns(Result<Library>.Success(_library));
        var page = Substitute.For<IPagedList<BookSummaryDto>>();
        page.Items.Returns([]);
        _booksService.GetPagedByLibrary(_library.Id, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<BookSortOrder>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<IPagedList<BookSummaryDto>>.Success(page));

        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(librariesService);
        _ctx.Services.AddSingleton(_booksService);
        _ctx.Services.AddSingleton(_workflow);
        _ctx.Services.AddSingleton(_stateService);
    }

    public ValueTask DisposeAsync() => _ctx.DisposeAsync();

    private async Task<IRenderedComponent<LibraryDetailPage>> RenderAsync(ViewMode viewMode)
    {
        _stateService.Save(_library.Id, new LibraryPageState(string.Empty, viewMode));
        var cut = _ctx.Render<LibraryDetailPage>(p => p.Add(c => c.LibraryId, _library.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Comics"));
        _booksService.ClearReceivedCalls();
        return cut;
    }

    private Task<Result<IPagedList<BookSummaryDto>>> ReceivedPageLoads(int count) =>
        _booksService.Received(count).GetPagedByLibrary(_library.Id, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<BookSortOrder>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());

    [Theory]
    [InlineData(ViewMode.Cards, 1)]
    [InlineData(ViewMode.List, 2)]
    public async Task MoveAsync_Should_ReloadTheBooks_WhenTheBookWasMoved(ViewMode viewMode, int expectedLoads)
    {
        var bookId = Guid.CreateVersion7();
        _workflow.ChooseAndMoveAsync(bookId, Arg.Any<CancellationToken>()).Returns(true);
        var cut = await RenderAsync(viewMode);

        await cut.InvokeAsync(() => cut.Instance.MoveAsync(bookId));

        await _workflow.Received(1).ChooseAndMoveAsync(bookId, Arg.Any<CancellationToken>());
        await ReceivedPageLoads(expectedLoads);
    }

    [Fact]
    public async Task MoveAsync_Should_NotReload_WhenTheBookWasNotMoved()
    {
        _workflow.ChooseAndMoveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        var cut = await RenderAsync(ViewMode.Cards);

        await cut.InvokeAsync(() => cut.Instance.MoveAsync(Guid.CreateVersion7()));

        await ReceivedPageLoads(0);
    }

    [Fact]
    public async Task MoveAsync_Should_IgnoreASecondRequest_WhenAMoveIsInProgress()
    {
        var pending = new TaskCompletionSource<bool>();
        _workflow.ChooseAndMoveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var cut = await RenderAsync(ViewMode.Cards);

        var first = cut.InvokeAsync(() => cut.Instance.MoveAsync(Guid.CreateVersion7()));
        await cut.InvokeAsync(() => cut.Instance.MoveAsync(Guid.CreateVersion7()));
        pending.SetResult(false);
        await first;

        await _workflow.Received(1).ChooseAndMoveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveAsync_Should_SwallowCancellation_WhenThePageIsDisposedDuringTheMove()
    {
        _workflow.ChooseAndMoveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException());
        var cut = await RenderAsync(ViewMode.Cards);

        var move = () => cut.InvokeAsync(() => cut.Instance.MoveAsync(Guid.CreateVersion7()));

        await move.Should().NotThrowAsync();
        await ReceivedPageLoads(0);
    }
}
