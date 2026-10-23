using Bunit;
using Domain.Errors;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages.Admin;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages;

public sealed class LegacyMigrationCardComponentTests
{
    private static (BunitContext Ctx, IRenderedComponent<LegacyMigrationCard> Cut, ISnackbar Snackbar) Render(ILegacyMigrationService service)
    {
        var snackbar = Substitute.For<ISnackbar>();
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(service);
        ctx.Services.AddSingleton(snackbar);
        return (ctx, ctx.Render<LegacyMigrationCard>(), snackbar);
    }

    [Fact]
    public async Task MigrateAsync_Should_StartTheMoveOfTheChosenNumberOfBooks_InTheBackground()
    {
        var service = Substitute.For<ILegacyMigrationService>();
        service.StartAsync(5, Arg.Any<CancellationToken>()).Returns(5);
        var (ctx, cut, snackbar) = Render(service);
        await using var _ = ctx;

        await cut.Find(".legacy-migration-count").ChangeAsync(new() { Value = "5" });
        await cut.Find(".legacy-migration-button").ClickAsync(new());

        snackbar.Received(1).Add("5 book(s) will be moved to the import folder in the background.", Severity.Success, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task MigrateAsync_Should_ShowTheError_WhenTheMigrationFails()
    {
        var service = Substitute.For<ILegacyMigrationService>();
        service.StartAsync(20, Arg.Any<CancellationToken>()).Returns(LegacyMigrationError.SourceNotFound);
        var (ctx, cut, snackbar) = Render(service);
        await using var _ = ctx;

        await cut.Find(".legacy-migration-button").ClickAsync(new());

        snackbar.Received(1).Add(LegacyMigrationError.SourceNotFound.Description!, Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }
}
