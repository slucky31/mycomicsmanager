using Application.Books.Covers;
using AwesomeAssertions;
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

public sealed class CloudinaryCoversCardComponentTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly ICloudinaryCoversService _service = Substitute.For<ICloudinaryCoversService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();

    public CloudinaryCoversCardComponentTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_service);
        _ctx.Services.AddSingleton(_snackbar);
        _service.CountToMoveAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(0));
    }

    public ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        return _ctx.DisposeAsync();
    }

    private IRenderedComponent<CloudinaryCoversCard> Render() => _ctx.Render<CloudinaryCoversCard>();

    private static AngleSharp.Dom.IElement Button(IRenderedComponent<CloudinaryCoversCard> cut, string text) =>
        cut.FindAll("button").Single(b => b.TextContent.Contains(text, StringComparison.Ordinal));

    [Fact]
    public async Task StartMoveAsync_Should_StartTheMove_AndKeepTheUnusedImagesClosed_WhileCoversAreLeft()
    {
        _service.CountToMoveAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(1284));
        _service.StartMoveAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(1290));
        var cut = Render();

        cut.Find(".cloudinary-covers-count").TextContent.Should().Contain("1284 books still use an old cover");
        Button(cut, "Find unused images").HasAttribute("disabled").Should().BeTrue();
        await Button(cut, "Move covers").ClickAsync(new());

        _snackbar.Received(1).Add("1290 cover(s) will be moved in the background.", Severity.Success, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task DeleteAsync_Should_DeleteTheUnusedImagesFound_AndShowWhatWasFreed()
    {
        var unused = new UnusedCovers(["covers/1", "covers/2", "digital-covers/3", "digital-covers/4"], 4_200_000);
        _service.FindUnusedAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(unused));
        _service.DeleteUnusedAsync(unused.PublicIds, Arg.Any<CancellationToken>()).Returns(Result.Success(new DeletedCovers(4, 4_200_000)));
        var cut = Render();

        cut.Markup.Should().Contain("Every book has its own cover.");
        await Button(cut, "Find unused images").ClickAsync(new());
        cut.Find(".cloudinary-covers-warning").TextContent.Should().Contain("4 images (4.2 MB) are used by no book.").And.Contain("… and 1 more");
        await Button(cut, "Delete 4 images").ClickAsync(new());

        cut.Find(".cloudinary-covers-success").TextContent.Should().Contain("4 images deleted, 4.2 MB freed.");
    }

    [Fact]
    public async Task Cancel_Should_CloseTheUnusedImages_WithoutDeletingThem()
    {
        _service.FindUnusedAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(new UnusedCovers(["covers/1"], 1_500)));
        var cut = Render();

        await Button(cut, "Find unused images").ClickAsync(new());
        cut.Find(".cloudinary-covers-warning").TextContent.Should().Contain("1 image (2 KB) is used by no book.");
        await Button(cut, "Cancel").ClickAsync(new());

        cut.FindAll(".cloudinary-covers-warning").Should().BeEmpty();
        await _service.DidNotReceiveWithAnyArgs().DeleteUnusedAsync(default!, Xunit.TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FindUnusedAsync_Should_ShowTheErrors_OfEveryAction()
    {
        _service.FindUnusedAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure<UnusedCovers>(BooksError.CoversNotListed));
        var cut = Render();

        await Button(cut, "Find unused images").ClickAsync(new());

        _snackbar.Received(1).Add(BooksError.CoversNotListed.Description!, Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }
}
