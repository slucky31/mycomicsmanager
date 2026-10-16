using AwesomeAssertions;
using Bunit;
using MudBlazor.Services;
using Web.Components.SharedComponents;
using Xunit;

namespace Web.Tests.Components.SharedComponents;

public sealed class StarRatingTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();

    public StarRatingTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
    }

    public async ValueTask DisposeAsync() => await _ctx.DisposeAsync();

    [Fact]
    public void StarRating_Should_ShowTheRatingAsAnImage_WhenReadOnly()
    {
        var cut = _ctx.Render<StarRating>(p => p.Add(c => c.Value, 4).Add(c => c.ReadOnly, true));

        cut.Find("[role=img]").GetAttribute("aria-label").Should().Be("Rated 4 out of 5");
        cut.FindAll(".star-rating-star").Should().HaveCount(5);
        cut.FindAll(".star-rating-star.star-rating-on").Should().HaveCount(4);
        cut.FindAll("button").Should().BeEmpty();
    }

    [Fact]
    public async Task StarRating_Should_ClearTheRating_WhenTheChosenStarIsChosenAgain()
    {
        var chosen = -1;
        var cut = _ctx.Render<StarRating>(p => p.Add(c => c.Value, 3).Add(c => c.ValueChanged, (int value) => chosen = value));

        await cut.Find("button[aria-label='3 stars']").ClickAsync(new());

        chosen.Should().Be(0);
    }
}
