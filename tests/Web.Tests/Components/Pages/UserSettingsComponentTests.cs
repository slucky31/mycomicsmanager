using AwesomeAssertions;
using Bunit;
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
        HealthCheckService healthCheckService, bool isAdmin = false)
    {
        var snackbar = Substitute.For<ISnackbar>();
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(healthCheckService);
        ctx.Services.AddSingleton(snackbar);
        ctx.Services.AddSingleton(Substitute.For<IFeatureToggleService>());
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

    [Fact]
    public async Task OnInitializedAsync_Should_RenderCheckStatus_WhenHealthy()
    {
        var healthCheckService = Substitute.For<HealthCheckService>();
        healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(CreateReport(HealthStatus.Healthy));

        var (ctx, cut, _) = await RenderAsync(healthCheckService);
        await using var _ = ctx;

        cut.Markup.Should().Contain("cloudinary");
        cut.Markup.Should().Contain("Healthy");
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
        cut.Find(".app-version").TextContent.Should().Be($"V{expected}");
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

        cut.Markup.Should().Contain("Unhealthy");
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

        var (ctx, _, snackbar) = await RenderAsync(healthCheckService);
        await using var _ = ctx;

        snackbar.Received().Add("Failed to load system health", Severity.Error,
            Arg.Any<Action<SnackbarOptions>?>(), Arg.Any<string?>());
    }
}
