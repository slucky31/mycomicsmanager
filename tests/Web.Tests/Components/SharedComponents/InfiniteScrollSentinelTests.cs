using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;
using Web.Components.SharedComponents;
using Xunit;

namespace Web.Tests.Components.SharedComponents;

public sealed class InfiniteScrollSentinelTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private int _loadMoreCalls;

    public InfiniteScrollSentinelTests() => _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

    public ValueTask DisposeAsync() => _ctx.DisposeAsync();

    private IRenderedComponent<InfiniteScrollSentinel> RenderSentinel(int itemCount = 20, string? scrollContainer = null) =>
        _ctx.Render<InfiniteScrollSentinel>(p => p
            .Add(c => c.ItemCount, itemCount)
            .Add(c => c.ScrollContainer, scrollContainer)
            .Add(c => c.OnLoadMore, () => _loadMoreCalls++));

    private int ObserveCalls => _ctx.JSInterop.Invocations["infiniteScroll.observe"].Count;

    [Fact]
    public void OnAfterRenderAsync_Should_ObserveTheSentinel_WithTheMethodThatLoadsMore()
    {
        var cut = RenderSentinel(scrollContainer: ".feeds-list");

        var observe = _ctx.JSInterop.VerifyInvoke("infiniteScroll.observe");
        var id = cut.Find(".infinite-scroll-sentinel").Id;
        id.Should().StartWith("scroll-sentinel-");
        observe.Arguments.Skip(1).Should().Equal(id, ".feeds-list", nameof(InfiniteScrollSentinel.LoadMoreAsync));
    }

    [Fact]
    public void OnAfterRenderAsync_Should_ObserveAgain_OnlyWhenMoreItemsAreShown()
    {
        var cut = RenderSentinel();

        cut.Render(p => p.Add(c => c.ItemCount, 20));
        ObserveCalls.Should().Be(1);

        cut.Render(p => p.Add(c => c.ItemCount, 40));
        ObserveCalls.Should().Be(2);
    }

    [Fact]
    public void OnAfterRenderAsync_Should_TryAgainAtTheNextRender_WhenTheScriptFails()
    {
        _ctx.JSInterop.SetupVoid("infiniteScroll.observe", _ => true).SetException(new JSException("boom"));
        var cut = RenderSentinel();

        cut.Render(p => p.Add(c => c.ItemCount, 20));

        ObserveCalls.Should().Be(2);
    }

    [Fact]
    public async Task LoadMoreAsync_Should_AskTheListForTheNextPage()
    {
        var cut = RenderSentinel();

        await cut.Instance.LoadMoreAsync();

        _loadMoreCalls.Should().Be(1);
    }

    [Fact]
    public async Task DisposeAsync_Should_StopObservingTheSentinel()
    {
        var cut = RenderSentinel();
        var id = cut.Find(".infinite-scroll-sentinel").Id;

        await cut.Instance.DisposeAsync();

        _ctx.JSInterop.VerifyInvoke("infiniteScroll.dispose").Arguments.Should().Equal(id);
    }

    [Fact]
    public async Task DisposeAsync_Should_IgnoreAScriptFailure()
    {
        _ctx.JSInterop.SetupVoid("infiniteScroll.dispose", _ => true).SetException(new JSDisconnectedException("gone"));
        var cut = RenderSentinel();

        var dispose = async () => await cut.Instance.DisposeAsync();

        await dispose.Should().NotThrowAsync();
    }
}
