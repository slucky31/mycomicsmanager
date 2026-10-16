using AwesomeAssertions;
using Bunit;
using Bunit.TestDoubles;
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

public sealed class AddLibraryComponentTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly ILibrariesService _librariesService = Substitute.For<ILibrariesService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly BunitNavigationManager _navigation;

    public AddLibraryComponentTests()
    {
        _librariesService.Create(Arg.Any<CreateLibraryRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Result<Library>.Success(Library.Create("Mangas", "#26A69A", "MenuBook", LibraryBookType.Physical, Guid.CreateVersion7()).Value!));

        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_librariesService);
        _ctx.Services.AddSingleton(_snackbar);
        _navigation = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        _navigation.NavigateTo("/libraries/add");
    }

    public async ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        await _ctx.DisposeAsync();
    }

    private IRenderedComponent<AddLibrary> Render() => _ctx.Render<AddLibrary>();

    [Fact]
    public void AddLibrary_Should_ShowTheFormOfANewLibrary_WithItsPreview()
    {
        var cut = Render();

        cut.Find("h1").TextContent.Should().Be("New library");
        cut.Find(".library-name").TextContent.Should().Be("New library");
        cut.Find(".library-count").TextContent.Should().Be("No books yet");
        cut.Find("label[for=library-name]").TextContent.Should().Be("Name *");
        cut.FindAll("input[name=library-type]").Should().HaveCount(2);
        cut.Find(".library-type-card-selected .library-type-card-title").TextContent.Should().Be("Physical books");
        cut.Find(".library-color[aria-pressed=true]").GetAttribute("aria-label").Should().Be("Indigo");
        cut.Find(".library-icon-row-name").TextContent.Should().Be("Collection");
        cut.Find(".library-icons-inline .icon-chooser-caption").TextContent.Should().Be("Suggested");
    }

    [Fact]
    public async Task SaveAsync_Should_CreateTheLibraryChosen_AndReturnToTheLibraries()
    {
        var cut = Render();

        await cut.Find("#library-name").InputAsync(new() { Value = "Mangas" });
        await cut.FindAll("input[name=library-type]")[1].ChangeAsync(new() { Value = "Digital" });
        await cut.Find(".library-color[aria-label=Teal]").ClickAsync(new());
        await cut.Find(".library-icons-inline button[aria-label=Book]").ClickAsync(new());

        cut.Find(".library-name").TextContent.Should().Be("Mangas");
        cut.Find(".library-type").TextContent.Should().Be("Digital");
        cut.Find(".library-color[aria-pressed=true]").GetAttribute("aria-label").Should().Be("Teal");
        cut.Find(".library-icons-inline button[aria-pressed=true]").GetAttribute("aria-label").Should().Be("Book");

        await cut.Find("form").SubmitAsync();

        await _librariesService.Received(1).Create(
            new CreateLibraryRequest("Mangas", "#26A69A", "MenuBook", LibraryBookType.Digital), Arg.Any<CancellationToken>());
        _snackbar.Received(1).Add("Mangas was created", Severity.Success);
        _navigation.Uri.Should().EndWith("/libraries/list");
    }

    [Fact]
    public async Task SaveAsync_Should_ShowTheErrorUnderTheName_AndFollowWhatIsTyped()
    {
        var cut = Render();

        await cut.Find("form").SubmitAsync();

        cut.Find("[role=alert]").TextContent.Should().Be("1 field needs your attention.");
        cut.Find("#library-name-error").TextContent.Should().Be("Name is required.");
        cut.Find("#library-name").GetAttribute("aria-invalid").Should().Be("true");
        await _librariesService.DidNotReceive().Create(Arg.Any<CreateLibraryRequest>(), Arg.Any<CancellationToken>());

        await cut.Find("#library-name").InputAsync(new() { Value = "Mangas" });

        cut.FindAll("[role=alert]").Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_Should_SayTheNameIsTaken_AndStayOnTheForm()
    {
        _librariesService.Create(Arg.Any<CreateLibraryRequest>(), Arg.Any<CancellationToken>()).Returns(_ => Result<Library>.Failure(LibrariesError.Duplicate));
        var cut = Render();
        await cut.Find("#library-name").InputAsync(new() { Value = "Mangas" });

        await cut.Find("form").SubmitAsync();

        _snackbar.Received(1).Add("Another library already has this name", Severity.Error);
        _navigation.Uri.Should().EndWith("/libraries/add");
        cut.Find(".form-footer button[type=submit]").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_Should_SayTheLibraryWasNotCreated_WhenTheCreationFails()
    {
        _librariesService.Create(Arg.Any<CreateLibraryRequest>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("db"));
        var cut = Render();
        await cut.Find("#library-name").InputAsync(new() { Value = "Mangas" });

        await cut.Find("form").SubmitAsync();

        _snackbar.Received(1).Add("Unable to create the library", Severity.Error);
    }

    [Fact]
    public async Task ChooseIcon_Should_OpenTheIconsInASheet_AndCloseItOnceChosen()
    {
        var cut = Render();

        await cut.Find(".library-icon-row").ClickAsync(new());
        cut.Find("[role=dialog]").GetAttribute("aria-label").Should().Be("Choose an icon");

        await cut.Find(".library-icons-sheet button[aria-label=Heart]").ClickAsync(new());

        cut.FindAll("[role=dialog]").Should().BeEmpty();
        cut.Find(".library-icon-row-name").TextContent.Should().Be("Heart");
    }

    [Fact]
    public async Task CloseIcons_Should_KeepTheIcon_WhenTheSheetIsClosed()
    {
        var cut = Render();
        await cut.Find(".library-icon-row").ClickAsync(new());

        await cut.Find("[role=dialog] button[aria-label=Close]").ClickAsync(new());

        cut.FindAll("[role=dialog]").Should().BeEmpty();
        cut.Find(".library-icon-row-name").TextContent.Should().Be("Collection");
    }
}
