using Application.Libraries.Summaries;
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

public sealed class EditLibraryComponentTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly ILibrariesService _librariesService = Substitute.For<ILibrariesService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly BunitNavigationManager _navigation;
    private readonly Library _library = Library.Create("Mangas", "#26A69A", "AutoStories", LibraryBookType.Physical, Guid.CreateVersion7()).Value!;

    public EditLibraryComponentTests()
    {
        _librariesService.GetById(Arg.Any<string?>()).Returns(_ => Result<Library>.Success(_library));
        _librariesService.GetSummaries(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(_ => Result<IReadOnlyList<LibrarySummary>>.Success(
            [new LibrarySummary(_library.Id, "Mangas", "#26A69A", "AutoStories", LibraryBookType.Physical, 38) { Covers = ["/a.webp", "/b.webp"] }]));
        _librariesService.Update(Arg.Any<UpdateLibraryRequest>(), Arg.Any<CancellationToken>()).Returns(_ => Result<Library>.Success(_library));

        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_librariesService);
        _ctx.Services.AddSingleton(_snackbar);
        _navigation = _ctx.Services.GetRequiredService<BunitNavigationManager>();
    }

    public async ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        await _ctx.DisposeAsync();
    }

    private IRenderedComponent<EditLibrary> Render() => _ctx.Render<EditLibrary>(p => p.Add(c => c.LibraryId, _library.Id.ToString()));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EditLibrary_Should_ShowLibraryNotFound_WhenTheLibraryCannotBeLoaded(bool throws)
    {
        if (throws)
        {
            _librariesService.GetById(Arg.Any<string?>()).ThrowsAsync(new InvalidOperationException("db"));
        }
        else
        {
            _librariesService.GetById(Arg.Any<string?>()).Returns(LibrariesError.NotFound);
        }

        var cut = Render();

        cut.Find(".form-state h2").TextContent.Should().Be("Library not found");
        _snackbar.Received(1).Add("Unable to load the library", Severity.Error);
    }

    [Fact]
    public async Task EditLibrary_Should_KeepTheNameOfTheImportedLibrary_AndSaveItsColor()
    {
        var imported = Library.Create("Imported", "#5C6BC0", "Inbox", LibraryBookType.Digital, Guid.CreateVersion7()).Value!;
        _librariesService.GetById(Arg.Any<string?>()).Returns(_ => Result<Library>.Success(imported));
        var cut = Render();

        var name = cut.Find("#library-name");
        name.HasAttribute("readonly").Should().BeTrue();
        cut.Find(".form-field-help").TextContent.Should().Be("The feed imports add their books here: this library keeps its name.");
        name.GetAttribute("aria-describedby").Should().Contain("library-name-help");
        await cut.Find("form").SubmitAsync();

        await _librariesService.Received(1).Update(Arg.Is<UpdateLibraryRequest>(r => r.Name == "Imported"), Arg.Any<CancellationToken>());
        cut.FindAll("#library-hidden").Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_Should_HideTheLibrary_WhenHideThisLibraryIsTurnedOn()
    {
        var cut = Render();

        await cut.Find("#library-hidden").ChangeAsync(new() { Value = true });
        await cut.Find("form").SubmitAsync();

        cut.Find(".library-card").ClassList.Should().Contain("library-card-hidden");
        cut.Find(".library-hidden-chip").TextContent.Should().Be("Hidden");
        await _librariesService.Received(1).Update(Arg.Is<UpdateLibraryRequest>(r => r.IsHidden == true), Arg.Any<CancellationToken>());
        await _librariesService.Received(1).GetSummaries(true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void EditLibrary_Should_ShowTheLibrary_WithItsBooksInThePreview_AndWithoutItsType()
    {
        var cut = Render();

        cut.Find("h1").TextContent.Should().Be("Edit library");
        cut.Find(".form-bar-subtitle").TextContent.Should().Be("Mangas");
        cut.Find("#library-name").GetAttribute("value").Should().Be("Mangas");
        cut.FindAll("input[name=library-type]").Should().BeEmpty();
        cut.Find(".library-form-note").TextContent.Should().Be("The type of a library cannot be changed.");
        cut.Find(".library-count").TextContent.Should().Be("38 books");
        cut.FindAll(".library-cover").Should().HaveCount(2);
        cut.Find(".library-color[aria-pressed=true]").GetAttribute("aria-label").Should().Be("Teal");
        cut.Find(".library-icon-row-name").TextContent.Should().Be("Auto stories");
        cut.Find(".library-icons-inline .icon-grid button").GetAttribute("aria-pressed").Should().Be("true");
    }

    [Fact]
    public void EditLibrary_Should_ShowNoBooksInThePreview_WhenTheyCannotBeLoaded()
    {
        _librariesService.GetSummaries(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(LibrariesError.BadRequest);

        var cut = Render();

        cut.Find(".library-count").TextContent.Should().Be("No books yet");
    }

    [Fact]
    public async Task SaveAsync_Should_SaveTheChanges_AndReturnToTheLibrary()
    {
        var cut = Render();

        await cut.Find("#library-name").InputAsync(new() { Value = "Manga" });
        await cut.Find(".library-color[aria-label=Amber]").ClickAsync(new());
        await cut.Find("form").SubmitAsync();

        cut.Find(".form-bar-subtitle").TextContent.Should().Be("Mangas");
        await _librariesService.Received(1).Update(
            new UpdateLibraryRequest(_library.Id.ToString(), "Manga", "#FFCA28", "AutoStories", IsHidden: false), Arg.Any<CancellationToken>());
        _snackbar.Received(1).Add("Manga was saved", Severity.Success);
        _navigation.Uri.Should().EndWith($"/libraries/{_library.Id}");
    }

    [Fact]
    public async Task SaveAsync_Should_NotSave_WhenTheNameIsEmpty()
    {
        var cut = Render();

        await cut.Find("#library-name").InputAsync(new() { Value = "" });
        await cut.Find("form").SubmitAsync();

        cut.Find("#library-name-error").TextContent.Should().Be("Name is required.");
        await _librariesService.DidNotReceive().Update(Arg.Any<UpdateLibraryRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_Should_ShowTheErrorOfTheColor_WhenItIsNotAColor()
    {
        var library = Library.Create("Old", "#5C6BC0", "Star", LibraryBookType.Physical, Guid.CreateVersion7()).Value!;
        library.Update("red", "Star");
        _librariesService.GetById(Arg.Any<string?>()).Returns(_ => Result<Library>.Success(library));
        var cut = Render();

        await cut.Find("form").SubmitAsync();

        cut.Find("#library-color-error").TextContent.Should().Be("Invalid color format.");
        cut.FindAll(".library-color[aria-pressed=true]").Should().BeEmpty();

        await cut.Find(".library-color[aria-label=Teal]").ClickAsync(new());

        cut.FindAll("#library-color-error").Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, "Another library already has this name")]
    [InlineData(false, "Unable to save the changes")]
    public async Task SaveAsync_Should_SayWhyTheChangesWereNotSaved(bool duplicate, string message)
    {
        _librariesService.Update(Arg.Any<UpdateLibraryRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Result<Library>.Failure(duplicate ? LibrariesError.Duplicate : LibrariesError.NotFound));
        var cut = Render();

        await cut.Find("form").SubmitAsync();

        _snackbar.Received(1).Add(message, Severity.Error);
        _navigation.Uri.Should().NotEndWith($"/libraries/{_library.Id}");
    }

    [Fact]
    public async Task SaveAsync_Should_SayTheChangesWereNotSaved_WhenTheSaveFails()
    {
        _librariesService.Update(Arg.Any<UpdateLibraryRequest>(), Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException());
        var cut = Render();

        await cut.Find("form").SubmitAsync();

        _snackbar.Received(1).Add("Unable to save the changes", Severity.Error);
    }
}
