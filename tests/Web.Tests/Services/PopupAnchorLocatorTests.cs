using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Web.Models;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class PopupAnchorLocatorTests : IAsyncDisposable
{
    private readonly IJSRuntime _js = Substitute.For<IJSRuntime>();
    private readonly FakeModule _module = new();
    private readonly PopupAnchorLocator _locator;

    public PopupAnchorLocatorTests()
    {
        _js.InvokeAsync<IJSObjectReference>("import", Arg.Any<CancellationToken>(), Arg.Any<object?[]?>()).Returns(_module);
        _locator = new PopupAnchorLocator(_js, NullLogger<PopupAnchorLocator>.Instance);
    }

    public async ValueTask DisposeAsync()
    {
        _module.Disconnected = false;
        await _locator.DisposeAsync();
        await _module.DisposeAsync();
    }

    private Task<PopupAnchor?> AnchorOfButtonAsync() => _locator.AnchorOfAsync("book-actions", TestContext.Current.CancellationToken);

    [Fact]
    public async Task AnchorOfAsync_Should_ReturnThePositionOfTheElement_AndLoadTheScriptOnce()
    {
        _module.Anchor = new PopupAnchor(10, 20, 54, 64, 1280, 800);

        var first = await AnchorOfButtonAsync();
        var second = await AnchorOfButtonAsync();

        first.Should().Be(_module.Anchor);
        second.Should().Be(_module.Anchor);
        _module.Requests.Should().Equal("book-actions", "book-actions");
        await _js.Received(1).InvokeAsync<IJSObjectReference>("import", Arg.Any<CancellationToken>(), Arg.Any<object?[]?>());
    }

    [Fact]
    public async Task AnchorOfAsync_Should_ReturnNull_WhenTheScriptCannotBeLoaded()
    {
        _js.InvokeAsync<IJSObjectReference>("import", Arg.Any<CancellationToken>(), Arg.Any<object?[]?>()).ThrowsAsync(new JSException("offline"));

        var anchor = await AnchorOfButtonAsync();

        anchor.Should().BeNull();
    }

    [Fact]
    public async Task DisposeAsync_Should_ReleaseTheScript_WhenItWasLoaded()
    {
        await AnchorOfButtonAsync();

        await _locator.DisposeAsync();

        _module.DisposeCount.Should().Be(1);
    }

    [Fact]
    public async Task DisposeAsync_Should_IgnoreTheError_WhenTheCircuitIsGone()
    {
        await AnchorOfButtonAsync();
        _module.Disconnected = true;

        var act = async () => await _locator.DisposeAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DisposeAsync_Should_DoNothing_WhenTheScriptWasNeverLoaded()
    {
        await _locator.DisposeAsync();

        _module.DisposeCount.Should().Be(0);
    }

    // popupAnchor.js: returns the anchor set by the test; once disconnected, it cannot be released.
    private sealed class FakeModule : IJSObjectReference
    {
        public PopupAnchor? Anchor { get; set; }
        public bool Disconnected { get; set; }
        public int DisposeCount { get; private set; }
        public List<string?> Requests { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Requests.Add(args?[0] as string);
            return ValueTask.FromResult((TValue)(object?)Anchor!);
        }

        public ValueTask DisposeAsync()
        {
            if (Disconnected)
            {
                return ValueTask.FromException(new JSDisconnectedException("gone"));
            }

            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }
}
