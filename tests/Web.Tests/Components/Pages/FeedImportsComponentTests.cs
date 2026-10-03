using Application.FeedImports.Arbitrate;
using AwesomeAssertions;
using Bunit;
using Domain.FeedImports;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages;
using Web.Models;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages;

public sealed class FeedImportsComponentTests
{
    private static FeedImportDecisionPageViewModel CreatePage(params string[] titles)
    {
        var items = titles
            .Select((title, i) => FeedImportDecisionViewModel.From(
                FeedImportDecision.Create(Guid.CreateVersion7(), i + 1, title, $"https://planete-bd.org/{i + 1}", null).Value!))
            .ToList();
        return new FeedImportDecisionPageViewModel(items, items.Count);
    }

    private static async Task<(BunitContext Ctx, IRenderedComponent<FeedImports> Cut, ISnackbar Snackbar)> RenderAsync(IFeedImportService service)
    {
        var snackbar = Substitute.For<ISnackbar>();
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(service);
        ctx.Services.AddSingleton(snackbar);

        ctx.Render<MudPopoverProvider>();
        var cut = ctx.Render<FeedImports>();
        await Task.Yield();

        return (ctx, cut, snackbar);
    }

    private static IFeedImportService CreateService(bool syncEnabled = true)
    {
        var service = Substitute.For<IFeedImportService>();
        service.IsSyncEnabled.Returns(syncEnabled);
        service.GetDecisionsAsync(Arg.Any<FeedImportDecisionStatus?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<FeedImportDecisionPageViewModel>.Success(CreatePage("Blacksad T3", "Largo Winch T20")));
        return service;
    }

    [Fact]
    public async Task LoadServerDataAsync_Should_RenderDecisions_WhenLoadSucceeds()
    {
        var service = CreateService();

        var (ctx, cut, _) = await RenderAsync(service);
        await using var _ = ctx;

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Blacksad T3"));
        cut.Markup.Should().Contain("Largo Winch T20");
        cut.Markup.Should().Contain("En attente");
        cut.Markup.Should().Contain("Synchroniser maintenant");
        await service.Received().GetDecisionsAsync(null, null, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadServerDataAsync_Should_ShowSnackbarError_WhenLoadFails()
    {
        var service = CreateService();
        service.GetDecisionsAsync(Arg.Any<FeedImportDecisionStatus?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<FeedImportDecisionPageViewModel>.Failure(new TError("feed:err", "boom")));

        var (ctx, cut, snackbar) = await RenderAsync(service);
        await using var _ = ctx;

        await cut.WaitForAssertionAsync(() =>
            snackbar.Received().Add("Impossible de charger les décisions d'import.", Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>()));
        cut.Markup.Should().Contain("Aucune décision d'import.");
    }

    [Fact]
    public async Task OnStatusChangedAsync_Should_ReloadWithStatusFilter_WhenStatusSelected()
    {
        var service = CreateService();

        var (ctx, cut, _) = await RenderAsync(service);
        await using var _ = ctx;
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Blacksad T3"));

        var select = cut.FindComponent<MudSelect<FeedImportDecisionStatus?>>();
        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(FeedImportDecisionStatus.Failed));

        await service.Received(1).GetDecisionsAsync(FeedImportDecisionStatus.Failed, null, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleDetail_Should_ShowHistory_WhenExpandButtonClicked()
    {
        var service = CreateService();

        var (ctx, cut, _) = await RenderAsync(service);
        await using var _ = ctx;
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Blacksad T3"));
        cut.Markup.Should().NotContain("Historique");

        await cut.FindAll("button[aria-label='Afficher le détail']")[0].ClickAsync(new());

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Historique"));
        cut.Markup.Should().Contain(FeedImportDecision.CreatedReason);
        cut.FindAll("button[aria-label='Masquer le détail']").Should().ContainSingle();
    }

    [Fact]
    public async Task SyncNowAsync_Should_TriggerSyncAndReload_WhenButtonClicked()
    {
        var service = CreateService();
        service.TriggerSync().Returns(Result.Success());

        var (ctx, cut, snackbar) = await RenderAsync(service);
        await using var _ = ctx;
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Blacksad T3"));

        await cut.InvokeAsync(() => cut.Instance.SyncNowAsync());

        service.Received(1).TriggerSync();
        snackbar.Received(1).Add(Arg.Is<string>(m => m.StartsWith("Synchronisation lancée", StringComparison.Ordinal)), Severity.Info, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        await service.Received(2).GetDecisionsAsync(Arg.Any<FeedImportDecisionStatus?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Render_Should_HideSyncButtonAndShowInfo_WhenSyncIsDisabled()
    {
        var service = CreateService(syncEnabled: false);

        var (ctx, cut, _) = await RenderAsync(service);
        await using var _ = ctx;

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Blacksad T3"));
        cut.Markup.Should().NotContain("Synchroniser maintenant");
        cut.Markup.Should().Contain("synchronisation Miniflux est désactivée");
    }

    private static FeedImportDecisionViewModel CreateProbableDuplicate()
    {
        var decision = FeedImportDecision.Create(Guid.CreateVersion7(), 99, "Blacksad - Tome 3", "https://planete-bd.org/99", null).Value!;
        decision.RequestArbitration(FeedImportArbitrationKind.ProbableDuplicate,
            [new DownloadCandidate("Blacksad T03", null, null, [new DownloadMirror("https://1fichier.com/?a", "1fichier.com")])],
            new ParsedComicTitle("Blacksad", null, 3), Guid.CreateVersion7(), "Doublon probable", FeedImportDecidedBy.Auto);
        return FeedImportDecisionViewModel.From(decision);
    }

    [Fact]
    public async Task ResolveAsync_Should_SendNotDuplicateAndReload_WhenUserRejectsProbableDuplicate()
    {
        var decision = CreateProbableDuplicate();
        var service = CreateService();
        service.GetDecisionsAsync(Arg.Any<FeedImportDecisionStatus?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<FeedImportDecisionPageViewModel>.Success(new FeedImportDecisionPageViewModel([decision], 1)));
        service.ResolveArbitrationAsync(decision.Id, FeedImportArbitrationAction.NotDuplicate, null, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var (ctx, cut, snackbar) = await RenderAsync(service);
        await using var _ = ctx;
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Blacksad - Tome 3"));
        await cut.FindAll("button[aria-label='Afficher le détail']")[0].ClickAsync(new());
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Pas un doublon"));
        cut.Markup.Should().Contain("1fichier.com");

        var button = cut.FindAll("button").First(b => b.TextContent.Contains("Pas un doublon", StringComparison.Ordinal));
        await button.ClickAsync(new());

        await service.Received(1).ResolveArbitrationAsync(decision.Id, FeedImportArbitrationAction.NotDuplicate, null, Arg.Any<CancellationToken>());
        snackbar.Received(1).Add("Décision mise à jour.", Severity.Success, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        await service.Received(2).GetDecisionsAsync(Arg.Any<FeedImportDecisionStatus?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveAsync_Should_ShowError_WhenResolutionFails()
    {
        var decision = CreateProbableDuplicate();
        var service = CreateService();
        service.ResolveArbitrationAsync(decision.Id, FeedImportArbitrationAction.ConfirmDuplicate, null, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(FeedImportError.InvalidStatusTransition));

        var (ctx, cut, snackbar) = await RenderAsync(service);
        await using var _ = ctx;

        await cut.InvokeAsync(() => cut.Instance.ResolveAsync(decision.Id, FeedImportArbitrationAction.ConfirmDuplicate, null));

        snackbar.Received(1).Add(FeedImportError.InvalidStatusTransition.Description!, Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }
}
