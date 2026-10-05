using Application.Books.MoveTargets;
using AwesomeAssertions;
using Bunit;
using Domain.Books;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Web.Components.Pages.Books;
using Web.Components.Pages.Dialogs;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Books;

public sealed class BookDetailComponentTests
{
    [Fact]
    public async Task LoadBookAsync_Should_ShowTheLoadError_WhenLoadingThrows()
    {
        var service = Substitute.For<IBooksService>();
        service.GetById(Arg.Any<string?>()).ThrowsAsync(new InvalidOperationException());
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(service);
        ctx.Services.AddSingleton(Substitute.For<IBookMoveService>());

        var cut = ctx.Render<BookDetail>(p => p.Add(c => c.BookId, Guid.CreateVersion7().ToString()));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Book not found"));
    }

    private static (BunitContext Ctx, IBookMoveService MoveService, IDialogService Dialogs, ISnackbar Snackbar, DigitalBook Book) CreateMoveContext()
    {
        var book = DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", null), Guid.CreateVersion7(), "/data/A/b.cbz", 1).Value!;
        var booksService = Substitute.For<IBooksService>();
        booksService.GetById(Arg.Any<string?>()).Returns(Result<Book>.Success(book));
        var moveService = Substitute.For<IBookMoveService>();
        var dialogs = Substitute.For<IDialogService>();
        var snackbar = Substitute.For<ISnackbar>();
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(booksService);
        ctx.Services.AddSingleton(moveService);
        ctx.Services.AddSingleton(dialogs);
        ctx.Services.AddSingleton(snackbar);
        return (ctx, moveService, dialogs, snackbar, book);
    }

    [Fact]
    public async Task MoveBookAsync_Should_MoveToChosenLibraryAndReload_WhenUserConfirms()
    {
        var (ctx, moveService, dialogs, snackbar, book) = CreateMoveContext();
        await using var _ = ctx;
        var targetId = Guid.CreateVersion7();
        moveService.GetTargetsAsync(book.Id, Arg.Any<CancellationToken>())
            .Returns(new BookMoveTargets([new BookMoveTarget(targetId, "BD", "#111111", "Bookmark", 2)], targetId));
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(DialogResult.Ok(targetId));
        dialogs.ShowAsync<MoveBookDialog>(Arg.Any<string?>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions?>()).Returns(dialog);
        moveService.MoveAsync(book.Id, targetId, Arg.Any<CancellationToken>()).Returns(Result<Book>.Success(book));

        var cut = ctx.Render<BookDetail>(p => p.Add(c => c.BookId, book.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Blacksad"));
        await cut.InvokeAsync(() => cut.Instance.MoveBookAsync());

        await moveService.Received(1).MoveAsync(book.Id, targetId, Arg.Any<CancellationToken>());
        snackbar.Received(1).Add("Book moved", Severity.Success, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task MoveBookAsync_Should_NotOpenDialog_WhenNoOtherLibraryOfSameType()
    {
        var (ctx, moveService, dialogs, snackbar, book) = CreateMoveContext();
        await using var _ = ctx;
        moveService.GetTargetsAsync(book.Id, Arg.Any<CancellationToken>()).Returns(new BookMoveTargets([], null));

        var cut = ctx.Render<BookDetail>(p => p.Add(c => c.BookId, book.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Blacksad"));
        await cut.InvokeAsync(() => cut.Instance.MoveBookAsync());

        await dialogs.DidNotReceiveWithAnyArgs().ShowAsync<MoveBookDialog>(default, default(DialogParameters)!, default);
        snackbar.Received(1).Add("No other library of the same type", Severity.Info, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        await moveService.DidNotReceive().MoveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveBookAsync_Should_ShowError_WhenMoveFails()
    {
        var (ctx, moveService, dialogs, snackbar, book) = CreateMoveContext();
        await using var _ = ctx;
        var targetId = Guid.CreateVersion7();
        moveService.GetTargetsAsync(book.Id, Arg.Any<CancellationToken>())
            .Returns(new BookMoveTargets([new BookMoveTarget(targetId, "BD", "#111111", "Bookmark", 0)], null));
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(DialogResult.Ok(targetId));
        dialogs.ShowAsync<MoveBookDialog>(Arg.Any<string?>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions?>()).Returns(dialog);
        moveService.MoveAsync(book.Id, targetId, Arg.Any<CancellationToken>()).Returns(Result<Book>.Failure(BooksError.FileAlreadyExists));

        var cut = ctx.Render<BookDetail>(p => p.Add(c => c.BookId, book.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Blacksad"));
        await cut.InvokeAsync(() => cut.Instance.MoveBookAsync());

        snackbar.Received(1).Add(BooksError.FileAlreadyExists.Description!, Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }
}
