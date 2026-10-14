using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Web.Components.SharedComponents;
using Web.Models;
using Xunit;

namespace Web.Tests.Components.SharedComponents;

public sealed class ImportJobCardComponentTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();

    public ImportJobCardComponentTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
    }

    public async ValueTask DisposeAsync() => await _ctx.DisposeAsync();

    private static ImportJobViewModel Job(string status, string display, int progress = 0) => new(
        Guid.CreateVersion7(), "Entre deux vies.cbz", 88_080_384, status, display, progress,
        DateTime.UtcNow, null, null, null, 0, 0);

    [Fact]
    public void Render_Should_ShowTheStepAndProgress_WhileTheImportRuns()
    {
        var cut = _ctx.Render<ImportJobCard>(p => p.Add(c => c.Job, Job("Converting", "Converting pages", 42)));

        cut.Find(".import-job-status").TextContent.Should().Contain("Converting pages");
        cut.Find(".import-job-step").TextContent.Should().Be("Step 3 of 6 · 42%");
        cut.Find(".import-job-progress-value").GetAttribute("style").Should().Contain("width: 42%");
        cut.FindAll(".import-job-link").Should().BeEmpty();
    }

    [Fact]
    public void Render_Should_LinkTheBook_WhenTheImportCompleted()
    {
        var bookId = Guid.CreateVersion7();
        var cut = _ctx.Render<ImportJobCard>(p => p.Add(c => c.Job, Job("Completed", "Imported", 100) with { DigitalBookId = bookId }));

        cut.Find(".import-job-link").GetAttribute("href").Should().Be($"/books/{bookId}");
        cut.FindAll(".import-job-step").Should().BeEmpty();
    }

    [Fact]
    public void Render_Should_ShowTheError_WhenTheImportFailed()
    {
        var cut = _ctx.Render<ImportJobCard>(p => p.Add(c => c.Job,
            Job("Failed", "Failed") with { ErrorMessage = "The PDF has no readable page.", ErrorStep = "Extracting" }));

        cut.Find(".import-job-error").TextContent.Should().Be("The PDF has no readable page.");
        cut.Find(".import-job-error-step").TextContent.Should().Be("Failed while: Extracting pages");
    }
}
