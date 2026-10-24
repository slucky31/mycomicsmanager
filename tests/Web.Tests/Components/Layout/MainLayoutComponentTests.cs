using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Layout;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Layout;

public sealed class MainLayoutComponentTests
{
    [Fact]
    public async Task MainLayout_Should_HideOnlyTheBottomBar_WhileImportingABookInfo()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(Substitute.For<IFeedImportService>());
        ctx.Services.AddSingleton(new FeedImportNotifier());
        ctx.Services.AddSingleton(new LibrariesNotifier());
        ctx.AddAuthorization().SetAuthorized("user@example.com");
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"/books/{Guid.CreateVersion7()}/find-details");

        var cut = ctx.Render<MainLayout>(parameters => parameters.Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "page"))));

        cut.FindAll("nav.side-nav").Should().ContainSingle();
        cut.FindAll("nav.bottom-nav").Should().BeEmpty();
        cut.Find("main.page-body").ClassList.Should().NotContain("with-bottom-bar");

        navigation.NavigateTo("/libraries/list");

        cut.WaitForAssertion(() => cut.FindAll("nav.bottom-nav").Should().ContainSingle());
        cut.FindAll("nav.side-nav").Should().ContainSingle();
        cut.Find("main.page-body").ClassList.Should().Contain("with-bottom-bar");
    }
}
