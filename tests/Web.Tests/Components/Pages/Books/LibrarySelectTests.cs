using AwesomeAssertions;
using Bunit;
using Domain.Libraries;
using MudBlazor.Services;
using Web.Components.Pages.Books;
using Web.Models;
using Web.Validators;
using Xunit;

namespace Web.Tests.Components.Pages.Books;

public sealed class LibrarySelectTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly BunitJSModuleInterop _module;
    private readonly LibraryUiDto _readBooks = Library("Read Books", "#2E7D32", "Star");
    private readonly LibraryUiDto _wishlist = Library("Wishlist", "#C62828", "Bookmark");
    private readonly List<Guid> _chosen = [];

    public LibrarySelectTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _module = _ctx.AddLibrarySelect();
    }

    public async ValueTask DisposeAsync() => await _ctx.DisposeAsync();

    private static LibraryUiDto Library(string name, string color, string icon) =>
        LibraryUiDto.Convert(Domain.Libraries.Library.Create(name, color, icon, LibraryBookType.Physical, Guid.CreateVersion7()).Value!);

    private IRenderedComponent<LibrarySelect> Render(Guid value, string? allLabel = null, string? error = null, bool compact = false) =>
        _ctx.Render<LibrarySelect>(p => p
            .Add(c => c.Id, "library")
            .Add(c => c.Libraries, [_readBooks, _wishlist])
            .Add(c => c.Value, value)
            .Add(c => c.ValueChanged, id => _chosen.Add(id))
            .Add(c => c.AllLabel, allLabel)
            .Add(c => c.Error, error)
            .Add(c => c.Compact, compact));

    [Fact]
    public void LibrarySelect_Should_ShowTheChosenLibrary_WithItsIconAndColor()
    {
        var cut = Render(_wishlist.Id);

        var button = cut.Find("#library");
        button.GetAttribute("aria-label").Should().Be("Library: Wishlist");
        button.GetAttribute("aria-expanded").Should().Be("false");
        button.QuerySelector(".library-select-value")!.TextContent.Should().Be("Wishlist");
        button.QuerySelector(".library-icon")!.GetAttribute("style").Should().Be("--library-color: #C62828;");
        cut.FindAll("[role=dialog]").Should().BeEmpty();
    }

    [Fact]
    public void LibrarySelect_Should_AskToChooseALibrary_AndShowTheError_WhenNoneIsChosen()
    {
        var cut = Render(Guid.Empty, error: "Choose a library");

        cut.Find(".library-select-placeholder").TextContent.Should().Be("Choose a library");
        cut.FindAll("#library .library-icon").Should().BeEmpty();
        cut.Find("#library").GetAttribute("aria-invalid").Should().Be("true");
        cut.Find("#library").GetAttribute("aria-describedby").Should().Be("library-error");
        cut.Find("#library-error").TextContent.Should().Be("Choose a library");
    }

    [Fact]
    public async Task OpenAsync_Should_ListTheLibraries_NextToTheButton_WithTheChosenOneChecked()
    {
        _module.Setup<PopupAnchor?>("anchorOf", "library").SetResult(new PopupAnchor(100, 200, 500, 256, 1280, 800));
        var cut = Render(_wishlist.Id);

        await cut.Find("#library").ClickAsync(new());

        var sheet = cut.Find("[role=dialog][aria-label='Choose a library']");
        sheet.GetAttribute("style").Should().Contain("--popup-left: 100px;");
        sheet.QuerySelectorAll(".action-sheet-item").Select(i => i.TextContent.Trim()).Should().Equal("Read Books", "Wishlist");
        sheet.QuerySelector(".action-sheet-item-selected")!.TextContent.Trim().Should().Be("Wishlist");
        sheet.QuerySelector(".action-sheet-item-icon")!.GetAttribute("style").Should().Be("color: #2E7D32;");
        cut.Find("#library").GetAttribute("aria-expanded").Should().Be("true");
    }

    [Fact]
    public async Task ChooseAsync_Should_GiveTheChosenLibrary_AndClose()
    {
        var cut = Render(_wishlist.Id);

        await cut.ChooseLibraryAsync("library", "Read Books");

        _chosen.Should().Equal(_readBooks.Id);
        cut.FindAll("[role=dialog]").Should().BeEmpty();
    }

    [Fact]
    public async Task ChooseAsync_Should_GiveNoLibrary_WhenEveryLibraryIsChosen()
    {
        var cut = Render(_wishlist.Id, allLabel: "All libraries", compact: true);
        cut.Find("#library").ClassList.Should().Contain("library-select-compact");

        await cut.ChooseLibraryAsync("library", "All libraries");

        _chosen.Should().Equal(Guid.Empty);
    }

    [Fact]
    public async Task Close_Should_CloseTheList_WithoutChoosing()
    {
        var cut = Render(Guid.Empty, allLabel: "All libraries");
        cut.Find(".library-select-value").TextContent.Should().Be("All libraries");
        await cut.Find("#library").ClickAsync(new());

        await cut.Find(".action-sheet-backdrop").ClickAsync(new());

        cut.FindAll("[role=dialog]").Should().BeEmpty();
        _chosen.Should().BeEmpty();
    }
}
