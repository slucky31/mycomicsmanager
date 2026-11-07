using AwesomeAssertions;
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

    private static ILegacyMigrationService CreateService(int remaining = 0)
    {
        var service = Substitute.For<ILegacyMigrationService>();
        service.CountRemainingAsync(Arg.Any<CancellationToken>()).Returns(remaining);
        return service;
    }

    [Fact]
    public async Task CountRemainingAsync_Should_ShowTheNumberOfFilesLeftInTheOldLibrary_WhenTheCardIsShown()
    {
        var (ctx, cut, _) = Render(CreateService(1234));
        await using var _ = ctx;

        cut.Find(".legacy-migration-remaining").TextContent.Should().Contain("1234 files left to import in the old library");
    }

    [Fact]
    public async Task CountRemainingAsync_Should_ShowTheNewNumber_WhenRefreshed()
    {
        var service = CreateService(2);
        var (ctx, cut, _) = Render(service);
        await using var _ = ctx;
        service.CountRemainingAsync(Arg.Any<CancellationToken>()).Returns(1);

        await cut.Find(".legacy-migration-refresh").ClickAsync(new());

        cut.Find(".legacy-migration-remaining").TextContent.Should().Contain("1 file left to import in the old library");
    }

    [Fact]
    public async Task CountRemainingAsync_Should_ShowTheErrorAndNoNumber_WhenTheOldLibraryCannotBeCounted()
    {
        var service = Substitute.For<ILegacyMigrationService>();
        service.CountRemainingAsync(Arg.Any<CancellationToken>()).Returns(LegacyMigrationError.SourceNotFound);
        var (ctx, cut, snackbar) = Render(service);
        await using var _ = ctx;

        cut.Find(".legacy-migration-remaining").TextContent.Should().NotContain("left to import");
        snackbar.Received(1).Add(LegacyMigrationError.SourceNotFound.Description!, Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task MigrateAsync_Should_StartTheMoveOfTheChosenNumberOfBooks_InTheBackground()
    {
        var service = CreateService();
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
        var service = CreateService();
        service.StartAsync(20, Arg.Any<CancellationToken>()).Returns(LegacyMigrationError.SourceNotFound);
        var (ctx, cut, snackbar) = Render(service);
        await using var _ = ctx;

        await cut.Find(".legacy-migration-button").ClickAsync(new());

        snackbar.Received(1).Add(LegacyMigrationError.SourceNotFound.Description!, Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }
}
