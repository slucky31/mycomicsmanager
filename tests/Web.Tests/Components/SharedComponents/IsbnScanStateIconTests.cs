using AwesomeAssertions;
using Bunit;
using Domain.Books;
using MudBlazor.Services;
using Web.Components.SharedComponents;
using Xunit;

namespace Web.Tests.Components.SharedComponents;

public sealed class IsbnScanStateIconTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();

    public IsbnScanStateIconTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
    }

    public ValueTask DisposeAsync() => _ctx.DisposeAsync();

    private IRenderedComponent<IsbnScanStateIcon> Render(IsbnScanState state, int candidateCount = 0, bool hideWhenKnown = false) =>
        _ctx.Render<IsbnScanStateIcon>(p => p
            .Add(c => c.State, state)
            .Add(c => c.CandidateCount, candidateCount)
            .Add(c => c.Isbn, state == IsbnScanState.HasIsbn ? "9782917237359" : null)
            .Add(c => c.HideWhenKnown, hideWhenKnown));

    [Fact]
    public void Render_Should_ShowAPlainQrCode_WhenTheIsbnIsKnown()
    {
        var cut = Render(IsbnScanState.HasIsbn);

        cut.Find("[aria-label='ISBN 9782917237359']").Should().NotBeNull();
        cut.FindAll(".mud-badge").Should().BeEmpty();
    }

    [Fact]
    public void Render_Should_ShowNothing_WhenTheIsbnIsKnownAndHidden()
    {
        Render(IsbnScanState.HasIsbn, hideWhenKnown: true).Markup.Trim().Should().BeEmpty();
    }

    [Theory]
    [InlineData(IsbnScanState.NotScanned, 0, "?")]
    [InlineData(IsbnScanState.Candidates, 2, "2")]
    public void Render_Should_FadeTheQrCodeAndBadgeIt_WhenTheIsbnIsMissing(IsbnScanState state, int candidateCount, string badge)
    {
        var cut = Render(state, candidateCount);

        cut.FindAll(".isbn-state-missing").Should().ContainSingle();
        cut.Find(".mud-badge").TextContent.Trim().Should().Be(badge);
    }

    [Fact]
    public void Render_Should_BadgeACross_WhenNoIsbnWasFound()
    {
        var cut = Render(IsbnScanState.NotFound);

        cut.Find(".mud-badge").QuerySelector("svg").Should().NotBeNull();
        cut.Find(".isbn-state-missing").GetAttribute("aria-label").Should().Be("No ISBN found in the pages");
    }
}
