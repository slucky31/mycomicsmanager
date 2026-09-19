using Application.Interfaces;
using AwesomeAssertions;
using Bunit;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages;
using Web.Components.SharedComponents;
using Web.Models;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages;

public sealed class ImportComponentTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    [Fact]
    public async Task PollJobsAsync_Should_NotExecute_WhenComponentIsDisposed()
    {
        // Arrange
        CancellationToken capturedToken = default;

        var library = Library.Create("Comics", "#5C6BC0", "CollectionsBookmark",
            LibraryBookType.Digital, Guid.CreateVersion7()).Value!;
        var pagedList = Substitute.For<IPagedList<Library>>();
        pagedList.Items.Returns(new List<Library> { library });

        var librariesService = Substitute.For<ILibrariesService>();
        librariesService
            .FilterBy(Arg.Any<string?>(), Arg.Any<LibrariesColumn?>(), Arg.Any<SortOrder?>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<IPagedList<Library>>.Success(pagedList));

        var importService = Substitute.For<IImportService>();
        importService
            .GetImportJobsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedToken = callInfo.Arg<CancellationToken>();
                return Task.FromResult(
                    Result<IReadOnlyList<ImportJobViewModel>>.Success(
                        (IReadOnlyList<ImportJobViewModel>)[]));
            });

        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(librariesService);
        ctx.Services.AddSingleton(importService);

        ctx.Render<MudBlazor.MudPopoverProvider>();

        // Act: render triggers OnInitializedAsync → LoadJobsAsync → GetImportJobsAsync(_pollingCts.Token)
        var cut = ctx.Render<Import>();

        capturedToken.CanBeCanceled.Should().BeTrue();
        capturedToken.IsCancellationRequested.Should().BeFalse();

        await cut.Instance.DisposeAsync();

        // Assert: CTS is cancelled — any queued PollJobsAsync invocation will abort immediately
        capturedToken.IsCancellationRequested.Should().BeTrue();
    }

    // ── Test infrastructure ───────────────────────────────────────────────────

    private static ImportJobViewModel CreateJob(
        string status = "Pending", string fileName = "comic.cbz", Guid? id = null) => new(
        Id: id ?? Guid.CreateVersion7(),
        OriginalFileName: fileName,
        OriginalFileSize: 1024,
        Status: status,
        StatusDisplay: status,
        StatusColor: Color.Default,
        ProgressPercent: 0,
        CreatedAt: DateTime.UtcNow,
        CompletedAt: null,
        ErrorMessage: null,
        ErrorStep: null,
        ConvertedImagesCount: 0,
        TotalImagesToConvert: 0);

    private static IBrowserFile CreateBrowserFile(string name)
    {
        var file = Substitute.For<IBrowserFile>();
        file.Name.Returns(name);
        file.OpenReadStream(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryStream([1, 2, 3]));
        return file;
    }

    private sealed class TestSetup(
        BunitContext ctx,
        IRenderedComponent<Import> cut,
        Library library,
        ILibrariesService librariesService,
        IImportService importService,
        ISnackbar snackbar) : IAsyncDisposable
    {
        public IRenderedComponent<Import> Cut { get; } = cut;
        public Library Library { get; } = library;
        public ILibrariesService LibrariesService { get; } = librariesService;
        public IImportService ImportService { get; } = importService;
        public ISnackbar Snackbar { get; } = snackbar;

        public ValueTask DisposeAsync() => ctx.DisposeAsync();
    }

    private static async Task<TestSetup> SetupAsync(
        IReadOnlyList<ImportJobViewModel>? initialJobs = null,
        Result<IPagedList<Library>>? librariesResult = null,
        Result<IReadOnlyList<ImportJobViewModel>>? jobsResult = null)
    {
        var library = Library.Create("Comics", "#5C6BC0", "CollectionsBookmark",
            LibraryBookType.Digital, s_userId).Value!;

        var librariesService = Substitute.For<ILibrariesService>();
        if (librariesResult is not null)
        {
            librariesService.FilterBy(Arg.Any<string?>(), Arg.Any<LibrariesColumn?>(), Arg.Any<SortOrder?>(),
                    Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(librariesResult);
        }
        else
        {
            var pagedList = Substitute.For<IPagedList<Library>>();
            pagedList.Items.Returns(new List<Library> { library });
            librariesService.FilterBy(Arg.Any<string?>(), Arg.Any<LibrariesColumn?>(), Arg.Any<SortOrder?>(),
                    Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Result<IPagedList<Library>>.Success(pagedList));
        }

        var importService = Substitute.For<IImportService>();
        importService.GetImportJobsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(jobsResult ?? Result<IReadOnlyList<ImportJobViewModel>>.Success(initialJobs ?? []));

        var snackbar = Substitute.For<ISnackbar>();

        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(snackbar);
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(librariesService);
        ctx.Services.AddSingleton(importService);

        ctx.Render<MudBlazor.MudPopoverProvider>();
        var cut = ctx.Render<Import>();
        await Task.Yield();

        return new TestSetup(ctx, cut, library, librariesService, importService, snackbar);
    }

    // ── LoadLibrariesAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task LoadLibrariesAsync_Should_SetLoadError_WhenServiceFails()
    {
        await using var setup = await SetupAsync(
            librariesResult: Result<IPagedList<Library>>.Failure(new TError("lib:err", "boom")));

        setup.Cut.Markup.Should().Contain("Impossible de charger les librairies.");
    }

    // ── LoadJobsAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadJobsAsync_Should_ShowSnackbarError_WhenServiceFails()
    {
        await using var setup = await SetupAsync(
            jobsResult: Result<IReadOnlyList<ImportJobViewModel>>.Failure(new TError("job:err", "boom")));

        setup.Snackbar.Received().Add(Arg.Any<string>(), Severity.Error, Arg.Any<Action<SnackbarOptions>?>(), Arg.Any<string?>());
    }

    // ── OnLibraryChangedAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task OnLibraryChangedAsync_Should_ReloadJobs_ForNewLibrary()
    {
        await using var setup = await SetupAsync();
        var select = setup.Cut.FindComponent<MudSelect<Guid>>();

        await setup.Cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(setup.Library.Id));

        await setup.ImportService.Received(2).GetImportJobsAsync(setup.Library.Id, Arg.Any<CancellationToken>());
    }

    // ── OnFilesSelectedAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task OnFilesSelectedAsync_Should_DoNothing_WhenNoFilesProvided()
    {
        await using var setup = await SetupAsync();

        await setup.Cut.InvokeAsync(() => setup.Cut.Instance.OnFilesSelectedAsync([]));

        await setup.ImportService.DidNotReceive()
            .UploadAndCreateJobAsync(Arg.Any<IBrowserFile>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnFilesSelectedAsync_Should_InsertJob_WhenUploadSucceeds()
    {
        await using var setup = await SetupAsync();
        var newJob = CreateJob(fileName: "new.cbz");
        setup.ImportService.UploadAndCreateJobAsync(Arg.Any<IBrowserFile>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ImportJobViewModel>.Success(newJob));

        var fileUpload = setup.Cut.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
        IReadOnlyList<IBrowserFile> files = [CreateBrowserFile("new.cbz")];
        await setup.Cut.InvokeAsync(() => fileUpload.Instance.FilesChanged.InvokeAsync(files));

        setup.Cut.Markup.Should().Contain("new.cbz");
        setup.Snackbar.Received().Add(Arg.Is<string>(s => s.Contains("envoyé")), Severity.Success,
            Arg.Any<Action<SnackbarOptions>?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task OnFilesSelectedAsync_Should_RecordError_WhenUploadFails()
    {
        await using var setup = await SetupAsync();
        setup.ImportService.UploadAndCreateJobAsync(Arg.Any<IBrowserFile>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ImportJobViewModel>.Failure(new TError("upl:err", "bad file")));

        var fileUpload = setup.Cut.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
        IReadOnlyList<IBrowserFile> files = [CreateBrowserFile("bad.cbz")];
        await setup.Cut.InvokeAsync(() => fileUpload.Instance.FilesChanged.InvokeAsync(files));

        setup.Cut.Markup.Should().Contain("bad file");
    }

    // ── DeleteJobAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteJobAsync_Should_RemoveJob_WhenSuccessful()
    {
        var job = CreateJob(status: "Completed");
        await using var setup = await SetupAsync(initialJobs: [job]);
        setup.ImportService.DeleteImportJobAsync(job.Id, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var card = setup.Cut.FindComponent<ImportJobCard>();
        await setup.Cut.InvokeAsync(() => card.Instance.OnDelete.InvokeAsync(job.Id));

        setup.Cut.Markup.Should().NotContain(job.OriginalFileName);
    }

    [Fact]
    public async Task DeleteJobAsync_Should_ShowSnackbarError_WhenFailed()
    {
        var job = CreateJob(status: "Completed");
        await using var setup = await SetupAsync(initialJobs: [job]);
        setup.ImportService.DeleteImportJobAsync(job.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new TError("del:err", "cannot delete")));

        var card = setup.Cut.FindComponent<ImportJobCard>();
        await setup.Cut.InvokeAsync(() => card.Instance.OnDelete.InvokeAsync(job.Id));

        setup.Cut.Markup.Should().Contain(job.OriginalFileName);
        setup.Snackbar.Received().Add("cannot delete", Severity.Error, Arg.Any<Action<SnackbarOptions>?>(), Arg.Any<string?>());
    }

    // ── ForceFailJobAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ForceFailJobAsync_Should_RefreshJobs_WhenSuccessful()
    {
        var job = CreateJob(status: "Extracting");
        await using var setup = await SetupAsync(initialJobs: [job]);
        setup.ImportService.ForceFailImportJobAsync(job.Id, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var card = setup.Cut.FindComponent<ImportJobCard>();
        await setup.Cut.InvokeAsync(() => card.Instance.OnForceFail.InvokeAsync(job.Id));

        setup.Snackbar.Received().Add("Import marqué comme échoué.", Severity.Warning,
            Arg.Any<Action<SnackbarOptions>?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task ForceFailJobAsync_Should_ShowSnackbarError_WhenFailed()
    {
        var job = CreateJob(status: "Extracting");
        await using var setup = await SetupAsync(initialJobs: [job]);
        setup.ImportService.ForceFailImportJobAsync(job.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new TError("ff:err", "cannot force-fail")));

        var card = setup.Cut.FindComponent<ImportJobCard>();
        await setup.Cut.InvokeAsync(() => card.Instance.OnForceFail.InvokeAsync(job.Id));

        setup.Snackbar.Received().Add("cannot force-fail", Severity.Error, Arg.Any<Action<SnackbarOptions>?>(), Arg.Any<string?>());
    }

    // ── DeleteTerminalJobsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteTerminalJobsAsync_Should_RemoveOnlyTerminalJobs()
    {
        var terminalJob = CreateJob(status: "Completed", fileName: "done.cbz");
        var activeJob = CreateJob(status: "Extracting", fileName: "active.cbz");
        await using var setup = await SetupAsync(initialJobs: [terminalJob, activeJob]);
        setup.ImportService.DeleteImportJobAsync(terminalJob.Id, Arg.Any<CancellationToken>()).Returns(Result.Success());

        await setup.Cut.InvokeAsync(setup.Cut.Instance.DeleteTerminalJobsAsync);

        setup.Cut.Markup.Should().NotContain("done.cbz");
        setup.Cut.Markup.Should().Contain("active.cbz");
    }

    [Fact]
    public async Task DeleteTerminalJobsAsync_Should_ShowSnackbarError_WhenSomeDeletesFail()
    {
        var terminalJob = CreateJob(status: "Failed", fileName: "failed.cbz");
        await using var setup = await SetupAsync(initialJobs: [terminalJob]);
        setup.ImportService.DeleteImportJobAsync(terminalJob.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new TError("del:err", "boom")));

        await setup.Cut.InvokeAsync(setup.Cut.Instance.DeleteTerminalJobsAsync);

        setup.Snackbar.Received().Add(Arg.Is<string>(s => s.Contains("Impossible de supprimer")), Severity.Error,
            Arg.Any<Action<SnackbarOptions>?>(), Arg.Any<string?>());
    }

    // ── Polling ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task PollJobsAsync_Should_UpdateJobs_WhenSuccessful()
    {
        var job = CreateJob(status: "Extracting", fileName: "polling.cbz");
        await using var setup = await SetupAsync(initialJobs: [job]);

        var updatedJob = job with { Status = "Completed", StatusDisplay = "Terminé" };
        setup.ImportService.GetImportJobsAsync(setup.Library.Id, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ImportJobViewModel>>.Success([updatedJob]));

        await setup.Cut.InvokeAsync(setup.Cut.Instance.PollJobsAsync);

        setup.Cut.Markup.Should().Contain("polling.cbz");
    }

    [Fact]
    public async Task PollJobsAsync_Should_LogOnly_WhenServiceFails()
    {
        var job = CreateJob(status: "Extracting");
        await using var setup = await SetupAsync(initialJobs: [job]);
        setup.ImportService.GetImportJobsAsync(setup.Library.Id, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ImportJobViewModel>>.Failure(new TError("poll:err", "boom")));

        var act = () => setup.Cut.InvokeAsync(setup.Cut.Instance.PollJobsAsync);

        await act.Should().NotThrowAsync();
    }
}
