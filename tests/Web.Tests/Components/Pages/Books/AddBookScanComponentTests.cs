using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages.Books;
using Web.Infrastructure;
using Xunit;

namespace Web.Tests.Components.Pages.Books;

public sealed class AddBookScanComponentTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();

    public AddBookScanComponentTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_snackbar);
    }

    public async ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        await _ctx.DisposeAsync();
    }

    [Fact]
    public async Task StartScanningAsync_Should_StartTheScannerOfTheCurrentRelease()
    {
        var scanner = _ctx.JSInterop.SetupModule(AppVersion.Versioned("./js/isbnScanner.js"));

        var cut = _ctx.Render<AddBookScan>();

        await cut.WaitForAssertionAsync(() => scanner.Invocations["startScan"].Should().ContainSingle());
        cut.Markup.Should().Contain("Position the barcode within the frame");
    }
}
