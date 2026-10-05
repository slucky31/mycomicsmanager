using AwesomeAssertions;
using Bunit;
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
}
