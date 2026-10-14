using AwesomeAssertions;
using Bunit;
using Bunit.TestDoubles;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages.Books;
using Web.Infrastructure;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Books;

public sealed class AddBookScanComponentTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly ILibrariesService _librariesService = Substitute.For<ILibrariesService>();
    private readonly Library _library = Library.Create("Read Books", "#2E7D32", "Star", LibraryBookType.Physical, Guid.CreateVersion7()).Value!;
    private readonly BunitJSModuleInterop _scanner;

    public AddBookScanComponentTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _scanner = _ctx.JSInterop.SetupModule(AppVersion.Versioned("./js/isbnScanner.js"));
        _scanner.Mode = JSRuntimeMode.Loose;
        _librariesService.GetById(_library.Id.ToString()).Returns(_ => Result<Library>.Success(_library));
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_snackbar);
        _ctx.Services.AddSingleton(_librariesService);
    }

    public async ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        await _ctx.DisposeAsync();
    }

    private IRenderedComponent<AddBookScan> Render()
    {
        _ctx.Services.GetRequiredService<BunitNavigationManager>().NavigateTo($"/books/add/scan?libraryId={_library.Id}");
        return _ctx.Render<AddBookScan>();
    }

    [Fact]
    public async Task StartScanningAsync_Should_StartTheScanner_AndRecallTheLibrary()
    {
        var cut = Render();

        await cut.WaitForAssertionAsync(() => _scanner.Invocations["startScan"].Should().ContainSingle());
        cut.WaitForAssertion(() => cut.Find(".scan-hint").TextContent.Should().Contain("Point the camera at the ISBN barcode"));
        cut.Find(".scan-target").TextContent.Should().Be("Adding to Read Books");
        cut.Find("a[aria-label='Close scanner']").GetAttribute("href").Should().Be($"/libraries/{_library.Id}");
        cut.Find(".scan-footer a").GetAttribute("href").Should().Be($"/books/add/manual?libraryId={_library.Id}");
        cut.FindAll("button[aria-label='Turn on the flashlight']").Should().BeEmpty();
    }

    [Fact]
    public async Task StartScanningAsync_Should_ShowASingleMessage_WhenTheCameraCannotBeUsed()
    {
        _scanner.SetupVoid("startScan", _ => true).SetException(new JSException("NotAllowedError: Permission denied"));

        var cut = Render();

        await cut.WaitForAssertionAsync(() => cut.Find("[role=alert] h2").TextContent.Should().Be("Camera not available"));
        cut.Markup.Should().NotContain("NotAllowedError");
        cut.Find(".scan-error a").GetAttribute("href").Should().Be($"/books/add/manual?libraryId={_library.Id}");
    }

    [Fact]
    public async Task RetryAsync_Should_StartTheScannerAgain()
    {
        var start = _scanner.SetupVoid("startScan", _ => true);
        start.SetException(new JSException("NotReadableError"));
        var cut = Render();
        await cut.WaitForAssertionAsync(() => cut.Find(".scan-error"));

        await cut.Find(".scan-error button").ClickAsync(new());

        _scanner.Invocations["startScan"].Should().HaveCount(2);
    }

    [Fact]
    public async Task OnScanErrorFromJs_Should_ShowTheCameraMessage()
    {
        var cut = Render();
        await cut.WaitForAssertionAsync(() => _scanner.Invocations["startScan"].Should().ContainSingle());

        await cut.InvokeAsync(() => cut.Instance.OnScanErrorFromJs("Video element not found"));

        cut.Find(".scan-error h2").TextContent.Should().Be("Camera not available");
    }

    [Fact]
    public async Task ToggleTorchAsync_Should_TurnOnTheFlashlight_WhenTheCameraHasOne()
    {
        _scanner.Setup<bool>("hasTorch").SetResult(true);
        _scanner.Setup<bool>("setTorch", _ => true).SetResult(true);
        var cut = Render();
        await cut.WaitForAssertionAsync(() => cut.Find("button[aria-label='Turn on the flashlight']"));

        await cut.Find("button[aria-label='Turn on the flashlight']").ClickAsync(new());

        cut.Find("button[aria-label='Turn off the flashlight']").GetAttribute("aria-pressed").Should().Be("true");
    }

    [Fact]
    public async Task OnIsbnScannedFromJsAsync_Should_LookUpTheBook_InTheLibrary()
    {
        var navigation = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        var cut = Render();
        await cut.WaitForAssertionAsync(() => _scanner.Invocations["startScan"].Should().ContainSingle());

        await cut.InvokeAsync(() => cut.Instance.OnIsbnScannedFromJsAsync("9782413015406"));

        navigation.Uri.Should().EndWith($"/books/add/form?isbn=9782413015406&libraryId={_library.Id}");
    }

    [Fact]
    public async Task OnIsbnScannedFromJsAsync_Should_StayOnTheScanner_WhenTheIsbnIsInvalid()
    {
        var navigation = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        var cut = Render();
        await cut.WaitForAssertionAsync(() => _scanner.Invocations["startScan"].Should().ContainSingle());

        await cut.InvokeAsync(() => cut.Instance.OnIsbnScannedFromJsAsync("9782413015407"));

        navigation.Uri.Should().NotContain("/books/add/form");
        _snackbar.Received(1).Add(Arg.Is<string>(m => m.Contains("9782413015407")), Severity.Error);
    }
}
