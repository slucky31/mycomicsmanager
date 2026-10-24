using AwesomeAssertions;
using Bunit;
using Domain.Primitives;
using Domain.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages;
using Web.Infrastructure;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages;

public sealed class UserSettingsComponentTests
{
    private static HealthReport CreateReport(HealthStatus status) => new(
        new Dictionary<string, HealthReportEntry>
        {
            ["cloudinary"] = new HealthReportEntry(status, "Cloudinary account is reachable.", TimeSpan.Zero, null, null)
        },
        TimeSpan.Zero);

    private static async Task<(BunitContext Ctx, IRenderedComponent<UserSettings> Cut, ISnackbar Snackbar)> RenderAsync(
        HealthCheckService healthCheckService, bool isAdmin = false, IHiddenLibrariesService? librariesService = null, LibrariesNotifier? librariesNotifier = null)
    {
        var snackbar = Substitute.For<ISnackbar>();
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(healthCheckService);
        ctx.Services.AddSingleton(snackbar);
        ctx.Services.AddSingleton(Substitute.For<IFeatureToggleService>());
        ctx.Services.AddSingleton(Substitute.For<ILegacyMigrationService>());
        ctx.Services.AddSingleton(Substitute.For<IComicInfoSyncService>());
        ctx.Services.AddSingleton(Substitute.For<IArchiveRefreshService>());
        ctx.Services.AddSingleton(librariesService ?? HidingLibraries(show: false));
        ctx.Services.AddSingleton(librariesNotifier ?? new LibrariesNotifier());
        var authorization = ctx.AddAuthorization();
        authorization.SetAuthorized("user");
        if (isAdmin)
        {
            authorization.SetRoles("Admin");
        }

        ctx.Render<MudPopoverProvider>();
        var cut = ctx.Render<UserSettings>();
        await Task.Yield();

        return (ctx, cut, snackbar);
    }

    private static IHiddenLibrariesService HidingLibraries(bool show)
    {
        var librariesService = Substitute.For<IHiddenLibrariesService>();
        librariesService.GetShowHiddenLibraries(Arg.Any<CancellationToken>()).Returns(Result<bool>.Success(show));
        librariesService.SetShowHiddenLibraries(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        return librariesService;
    }

    private static HealthCheckService Healthy()
    {
        var healthCheckService = Substitute.For<HealthCheckService>();
        healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(CreateReport(HealthStatus.Healthy));
        return healthCheckService;
    }

    [Fact]
    public async Task LoadShowHiddenLibrariesAsync_Should_CheckTheSwitch_WhenTheUserShowsTheHiddenLibraries()
    {
        var (ctx, cut, _) = await RenderAsync(Healthy(), librariesService: HidingLibraries(show: true));
        await using var _ = ctx;

        var input = cut.Find(".settings-hidden-libraries input[role=switch]");
        input.HasAttribute("checked").Should().BeTrue();
        input.HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public async Task LoadShowHiddenLibrariesAsync_Should_ShowAnError_WhenTheSettingCannotBeLoaded()
    {
        var librariesService = Substitute.For<IHiddenLibrariesService>();
        librariesService.GetShowHiddenLibraries(Arg.Any<CancellationToken>()).Returns(Result<bool>.Failure(UsersError.NotFound));

        var (ctx, cut, snackbar) = await RenderAsync(Healthy(), librariesService: librariesService);
        await using var _ = ctx;

        cut.Find(".settings-hidden-libraries input[role=switch]").HasAttribute("checked").Should().BeFalse();
        snackbar.Received(1).Add("Failed to load the setting of the hidden libraries", Severity.Error);
    }

    [Fact]
    public async Task SetShowHiddenLibrariesAsync_Should_SaveTheSetting_AndRefreshTheNavigation()
    {
        var librariesService = HidingLibraries(show: false);
        var notifier = new LibrariesNotifier();
        var notified = 0;
        notifier.LibrariesChanged += (_, _) => notified++;
        var (ctx, cut, _) = await RenderAsync(Healthy(), librariesService: librariesService, librariesNotifier: notifier);
        await using var _ = ctx;

        await cut.Find(".settings-hidden-libraries input[role=switch]").ChangeAsync(new() { Value = true });

        await librariesService.Received(1).SetShowHiddenLibraries(true, Arg.Any<CancellationToken>());
        cut.Find(".settings-hidden-libraries input[role=switch]").HasAttribute("checked").Should().BeTrue();
        notified.Should().Be(1);
    }

    [Fact]
    public async Task SetShowHiddenLibrariesAsync_Should_KeepTheSetting_WhenItCannotBeSaved()
    {
        var librariesService = HidingLibraries(show: false);
        librariesService.SetShowHiddenLibraries(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Result.Failure(UsersError.NotFound));
        var notifier = new LibrariesNotifier();
        var notified = 0;
        notifier.LibrariesChanged += (_, _) => notified++;
        var (ctx, cut, snackbar) = await RenderAsync(Healthy(), librariesService: librariesService, librariesNotifier: notifier);
        await using var _ = ctx;

        await cut.Find(".settings-hidden-libraries input[role=switch]").ChangeAsync(new() { Value = true });

        cut.Find(".settings-hidden-libraries input[role=switch]").HasAttribute("checked").Should().BeFalse();
        snackbar.Received(1).Add("Failed to save the setting of the hidden libraries", Severity.Error);
        notified.Should().Be(0);
    }

    [Fact]
    public async Task OnInitializedAsync_Should_RenderCheckStatus_WhenHealthy()
    {
        var healthCheckService = Substitute.For<HealthCheckService>();
        healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(CreateReport(HealthStatus.Healthy));

        var (ctx, cut, _) = await RenderAsync(healthCheckService);
        await using var _ = ctx;

        cut.Find(".settings-health-chip").TextContent.Should().Contain("1 of 1 healthy");
        cut.FindAll(".settings-health-detail").Should().BeEmpty();

        await cut.Find("button[aria-controls='settings-health-detail']").ClickAsync(new());

        cut.Find("button[aria-controls='settings-health-detail']").GetAttribute("aria-expanded").Should().Be("true");
        cut.Find(".settings-health-check").TextContent.Should().Contain("cloudinary").And.Contain("Healthy");
    }

    [Fact]
    public async Task OnInitializedAsync_Should_RenderApplicationVersion()
    {
        var healthCheckService = Substitute.For<HealthCheckService>();
        healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(CreateReport(HealthStatus.Healthy));

        var (ctx, cut, _) = await RenderAsync(healthCheckService);
        await using var _ = ctx;

        var expected = typeof(AppVersion).Assembly.GetName().Version!.ToString(3);
        cut.Find(".app-version").TextContent.Should().Be(expected);
    }

    [Fact]
    public async Task OnInitializedAsync_Should_RenderCheckStatus_WhenUnhealthy()
    {
        var healthCheckService = Substitute.For<HealthCheckService>();
        healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(CreateReport(HealthStatus.Unhealthy));

        var (ctx, cut, _) = await RenderAsync(healthCheckService);
        await using var _ = ctx;

        cut.Find(".settings-health-chip.health-unhealthy").TextContent.Should().Contain("0 of 1 healthy");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UserSettings_Should_ShowTheAdministrationSection_OnlyToAdmins(bool isAdmin)
    {
        var healthCheckService = Substitute.For<HealthCheckService>();
        healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(CreateReport(HealthStatus.Healthy));

        var (ctx, cut, _) = await RenderAsync(healthCheckService, isAdmin);
        await using var _ = ctx;

        cut.FindAll(".administration").Count.Should().Be(isAdmin ? 1 : 0);
        cut.FindAll("a[href='/admin/log-levels']").Count.Should().Be(isAdmin ? 1 : 0);
        cut.FindAll("a[href='/hangfire'][target='_blank']").Count.Should().Be(isAdmin ? 1 : 0);
    }

    [Fact]
    public async Task OnInitializedAsync_Should_ShowSnackbarError_WhenHealthCheckServiceThrows()
    {
        var healthCheckService = Substitute.For<HealthCheckService>();
        healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns<Task<HealthReport>>(_ => throw new InvalidOperationException("boom"));

        var (ctx, cut, snackbar) = await RenderAsync(healthCheckService);
        await using var _ = ctx;

        cut.Find(".settings-health-chip").TextContent.Should().Contain("Unavailable");

        snackbar.Received().Add("Failed to load system health", Severity.Error,
            Arg.Any<Action<SnackbarOptions>?>(), Arg.Any<string?>());
    }
}
