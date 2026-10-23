using Bunit;
using Domain.Books;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages.Admin;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages;

public sealed class ComicInfoSyncCardComponentTests
{
    private static (BunitContext Ctx, IRenderedComponent<ComicInfoSyncCard> Cut, ISnackbar Snackbar) Render(IComicInfoSyncService service)
    {
        var snackbar = Substitute.For<ISnackbar>();
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(service);
        ctx.Services.AddSingleton(snackbar);
        return (ctx, ctx.Render<ComicInfoSyncCard>(), snackbar);
    }

    [Theory]
    [InlineData(12, "12 book(s) will be updated in the background.", Severity.Success)]
    [InlineData(0, "No digital book to update.", Severity.Info)]
    public async Task StartAsync_Should_StartTheSync_AndShowHowManyBooksItUpdates(int count, string message, Severity severity)
    {
        var service = Substitute.For<IComicInfoSyncService>();
        service.StartAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(count));
        var (ctx, cut, snackbar) = Render(service);
        await using var _ = ctx;

        await cut.Find(".comicinfo-sync-button").ClickAsync(new());

        snackbar.Received(1).Add(message, severity, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task StartAsync_Should_ShowTheError_WhenTheSyncCannotStart()
    {
        var service = Substitute.For<IComicInfoSyncService>();
        service.StartAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(BooksError.BadRequest));
        var (ctx, cut, snackbar) = Render(service);
        await using var _ = ctx;

        await cut.Find(".comicinfo-sync-button").ClickAsync(new());

        snackbar.Received(1).Add(BooksError.BadRequest.Description!, Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }
}
