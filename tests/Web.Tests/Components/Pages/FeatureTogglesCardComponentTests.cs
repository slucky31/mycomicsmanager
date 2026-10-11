using Application.Settings;
using AwesomeAssertions;
using Bunit;
using Domain.Primitives;
using Domain.Settings;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages.Admin;
using Web.Models;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages;

public sealed class FeatureTogglesCardComponentTests
{
    private static IReadOnlyList<FeatureToggleViewModel> Toggles(bool feedImportEnabled) =>
    [
        FeatureToggleViewModel.From(new FeatureToggleState(FeatureToggle.FeedImport, feedImportEnabled, false)),
        FeatureToggleViewModel.From(new FeatureToggleState(FeatureToggle.IsbnOcr, true, true)),
    ];

    private static (BunitContext Ctx, IRenderedComponent<FeatureTogglesCard> Cut, ISnackbar Snackbar) Render(IFeatureToggleService service)
    {
        var snackbar = Substitute.For<ISnackbar>();
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(service);
        ctx.Services.AddSingleton(snackbar);
        return (ctx, ctx.Render<FeatureTogglesCard>(), snackbar);
    }

    [Fact]
    public async Task OnInitialized_Should_ListTheFeatures_WithTheirConfigurationAndOverride()
    {
        var service = Substitute.For<IFeatureToggleService>();
        service.GetToggles().Returns(Toggles(feedImportEnabled: true));

        var (ctx, cut, _) = Render(service);
        await using var _ = ctx;

        cut.Markup.Should().Contain("Feed import").And.Contain("Configured off (FeedImport:Enabled)").And.Contain("ISBN reading (OCR)");
        cut.FindAll(".feature-toggle").Should().HaveCount(2);
        cut.Markup.Should().Contain("overridden");
    }

    [Fact]
    public async Task SetAsync_Should_SaveAndReport_WhenChangeSucceeds()
    {
        var service = Substitute.For<IFeatureToggleService>();
        service.GetToggles().Returns(Toggles(feedImportEnabled: false), Toggles(feedImportEnabled: true));
        service.SetAsync(FeatureToggle.FeedImport, true, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var (ctx, cut, snackbar) = Render(service);
        await using var _ = ctx;

        await cut.InvokeAsync(() => cut.Instance.SetAsync(FeatureToggle.FeedImport, true));

        await service.Received(1).SetAsync(FeatureToggle.FeedImport, true, Arg.Any<CancellationToken>());
        snackbar.Received(1).Add("Feed import turned on.", Severity.Success, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        service.Received(2).GetToggles();
    }

    [Fact]
    public async Task SetAsync_Should_ShowError_AndReadBackTheState_WhenChangeFails()
    {
        var service = Substitute.For<IFeatureToggleService>();
        service.GetToggles().Returns(Toggles(feedImportEnabled: false));
        var error = FeatureToggleError.MissingConfiguration("Miniflux:ApiKey is required.");
        service.SetAsync(FeatureToggle.FeedImport, true, Arg.Any<CancellationToken>()).Returns(Result.Failure(error));

        var (ctx, cut, snackbar) = Render(service);
        await using var _ = ctx;

        await cut.InvokeAsync(() => cut.Instance.SetAsync(FeatureToggle.FeedImport, true));

        snackbar.Received(1).Add(error.Description!, Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        service.Received(2).GetToggles();
    }
}
