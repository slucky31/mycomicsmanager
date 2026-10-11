using Application.Libraries.Summaries;
using AwesomeAssertions;
using Bunit;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Layout;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Layout;

public sealed class NavBarComponentTests
{
    [Fact]
    public async Task RefreshArbitrationCountAsync_Should_ShowCountFromOwnScope_WhenSignedIn()
    {
        var service = Substitute.For<IFeedImportService>();
        service.CountAwaitingArbitrationAsync(Arg.Any<CancellationToken>()).Returns(3);
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddScoped(_ => service);
        ctx.Services.AddSingleton(new FeedImportNotifier());
        ctx.AddAuthorization().SetAuthorized("user@example.com");

        var cut = ctx.Render<NavBar>();

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("3 decisions awaiting arbitration"));
    }

    [Fact]
    public async Task NavBar_Should_NotShowTheLogsLink_WhenSignedInAsAdmin()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(Substitute.For<IFeedImportService>());
        ctx.Services.AddSingleton(new FeedImportNotifier());
        ctx.AddAuthorization().SetAuthorized("admin").SetRoles("Admin");

        var cut = ctx.Render<NavBar>();

        cut.FindAll("a[href='/admin/log-levels']").Should().BeEmpty();
    }

    [Fact]
    public async Task RefreshLibrariesAsync_Should_ListTheUserLibraries_AndMarkTheCurrentOne()
    {
        var current = new LibrarySummary(Guid.CreateVersion7(), "Read Books", "#2E7D32", "Star", LibraryBookType.Physical, 24);
        var other = new LibrarySummary(Guid.CreateVersion7(), "MesEbooks", "#5C6BC0", "Bookmark", LibraryBookType.Digital, 1);
        var libraries = Substitute.For<ILibrariesService>();
        libraries.GetSummaries(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<LibrarySummary>>.Success([current, other]));
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(Substitute.For<IFeedImportService>());
        ctx.Services.AddScoped(_ => libraries);
        ctx.Services.AddSingleton(new FeedImportNotifier());
        ctx.AddAuthorization().SetAuthorized("user@example.com");
        ctx.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo($"/libraries/{current.Id}");

        var cut = ctx.Render<NavBar>();

        await cut.WaitForAssertionAsync(() => cut.FindAll("a.side-nav-library").Should().HaveCount(2));
        var currentLink = cut.Find($"a.side-nav-library[href='/libraries/{current.Id}']");
        currentLink.GetAttribute("aria-current").Should().Be("page");
        currentLink.GetAttribute("aria-label").Should().Be("Read Books, 24 books");
        cut.Find($"a.side-nav-library[href='/libraries/{other.Id}']").HasAttribute("aria-current").Should().BeFalse();
        cut.Find("a.side-nav-item[href='/libraries/list']").ClassList.Should().Contain("active-parent");
    }

    [Fact]
    public async Task RefreshLibrariesAsync_Should_HideTheLibraries_WhenTheyCannotBeLoaded()
    {
        var libraries = Substitute.For<ILibrariesService>();
        libraries.GetSummaries(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<LibrarySummary>>.Failure(LibrariesError.ValidationError));
        var feeds = Substitute.For<IFeedImportService>();
        feeds.CountAwaitingArbitrationAsync(Arg.Any<CancellationToken>()).Returns(1);
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddScoped(_ => feeds);
        ctx.Services.AddScoped(_ => libraries);
        ctx.Services.AddSingleton(new FeedImportNotifier());
        ctx.AddAuthorization().SetAuthorized("user@example.com");

        var cut = ctx.Render<NavBar>();

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("1 decision awaiting arbitration"));
        cut.FindAll("a.side-nav-library").Should().BeEmpty();
        cut.Find("a.side-nav-item[href='/libraries/list']").ClassList.Should().Contain("active");
    }
}
