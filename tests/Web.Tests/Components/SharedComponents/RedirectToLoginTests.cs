using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Web.Components.SharedComponents;
using Xunit;

namespace Web.Tests.Components.SharedComponents;

public sealed class RedirectToLoginTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();

    public async ValueTask DisposeAsync() => await _ctx.DisposeAsync();

    [Fact]
    public void OnInitialized_Should_SendTheUserToAuth0_AndBackToTheRequestedPage()
    {
        var navigation = _ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/series/42?from=library");

        _ctx.Render<RedirectToLogin>();

        navigation.Uri.Should().Be("http://localhost/Account/Login?redirectUri=%2Fseries%2F42%3Ffrom%3Dlibrary");
    }
}
