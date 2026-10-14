using AwesomeAssertions;
using Bunit;
using Bunit.TestDoubles;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages.Books;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Books;

public sealed class AddBookManualComponentTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly Library _library = Library.Create("Read Books", "#2E7D32", "Star", LibraryBookType.Physical, Guid.CreateVersion7()).Value!;
    private readonly BunitNavigationManager _navigation;

    public AddBookManualComponentTests()
    {
        var librariesService = Substitute.For<ILibrariesService>();
        librariesService.GetById(_library.Id.ToString()).Returns(_ => Result<Library>.Success(_library));
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(librariesService);
        _navigation = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        _navigation.NavigateTo($"/books/add/manual?libraryId={_library.Id}");
    }

    public ValueTask DisposeAsync() => _ctx.DisposeAsync();

    [Fact]
    public void AddBookManual_Should_RecallTheLibrary_AndExplainTheField()
    {
        var cut = _ctx.Render<AddBookManual>();

        cut.Find(".add-bar-subtitle").TextContent.Should().Be("Adding to Read Books");
        cut.Find("a[aria-label='Back']").GetAttribute("href").Should().Be($"/libraries/{_library.Id}");
        cut.Find("label[for=isbn]").TextContent.Should().Be("ISBN");
        cut.Find("#isbn").GetAttribute("aria-describedby").Should().Be("isbn-help");
    }

    [Fact]
    public async Task LookUp_Should_ShowTheErrorUnderTheField_WhenTheIsbnIsInvalid()
    {
        var cut = _ctx.Render<AddBookManual>();
        await cut.Find("#isbn").InputAsync(new() { Value = "978-2-413-01540-7" });

        await cut.Find("form").SubmitAsync();

        var input = cut.Find("#isbn");
        input.GetAttribute("aria-invalid").Should().Be("true");
        input.GetAttribute("aria-describedby").Should().Be("isbn-error isbn-help");
        cut.Find("#isbn-error").TextContent.Should().Be("Enter a valid ISBN: 10 or 13 digits");
        _navigation.Uri.Should().EndWith($"/books/add/manual?libraryId={_library.Id}");
    }

    [Fact]
    public async Task LookUp_Should_LookUpTheBook_WithTheDashesAndSpacesRemoved()
    {
        var cut = _ctx.Render<AddBookManual>();
        await cut.Find("#isbn").InputAsync(new() { Value = "978-2-413 01540-6" });

        await cut.Find("form").SubmitAsync();

        _navigation.Uri.Should().EndWith($"/books/add/form?isbn=9782413015406&libraryId={_library.Id}");
    }
}
