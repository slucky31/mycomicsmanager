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
using Web.Components.Pages.Dialogs;
using Web.Components.SharedComponents;
using Web.Models;
using Web.Services;
using Web.Tests.Components.Pages.Books;
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
        ctx.AddLibrarySelect();
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

    // Owns its bUnit context, so that it is disposed with the setup.
    private sealed class TestSetup(
        Library library,
        ILibrariesService librariesService,
        IImportService importService,
        ISnackbar snackbar,
        IDialogService dialogService) : IAsyncDisposable
    {
        public BunitContext Context { get; } = new();
        public IRenderedComponent<Import> Cut { get; set; } = default!;
        public Library Library { get; } = library;
        public ILibrariesService LibrariesService { get; } = librariesService;
        public IImportService ImportService { get; } = importService;
        public ISnackbar Snackbar { get; } = snackbar;
        public IDialogService DialogService { get; } = dialogService;

        public void GivenConfirmation(bool confirmed)
        {
            var dialog = Substitute.For<IDialogReference>();
            dialog.Result.Returns(confirmed ? DialogResult.Ok(true) : DialogResult.Cancel());
            DialogService.ShowAsync<ConfirmationDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
                .Returns(dialog);
        }

        public ValueTask DisposeAsync() => Context.DisposeAsync();
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
        var dialogService = Substitute.For<IDialogService>();

        var setup = new TestSetup(library, librariesService, importService, snackbar, dialogService);
        var ctx = setup.Context;
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(snackbar);
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(dialogService);
        ctx.AddLibrarySelect();
        ctx.Services.AddSingleton(librariesService);
        ctx.Services.AddSingleton(importService);

        ctx.Render<MudBlazor.MudPopoverProvider>();
        setup.Cut = ctx.Render<Import>();
        await Task.Yield();

        return setup;
    }

    // ── LoadLibrariesAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task LoadLibrariesAsync_Should_SetLoadError_WhenServiceFails()
    {
        await using var setup = await SetupAsync(
            librariesResult: Result<IPagedList<Library>>.Failure(new TError("lib:err", "boom")));

        setup.Cut.Markup.Should().Contain("Unable to load the libraries.");
    }

    [Fact]
    public async Task OnInitializedAsync_Should_ChooseTheImportedLibrary_WhenNoLibraryIsAsked()
    {
        var comics = Library.Create("Comics", "#5C6BC0", "CollectionsBookmark", LibraryBookType.Digital, s_userId).Value!;
        var imported = Library.Create("Imported", "#5C6BC0", "Inbox", LibraryBookType.Digital, s_userId).Value!;
        var pagedList = Substitute.For<IPagedList<Library>>();
        pagedList.Items.Returns(new List<Library> { comics, imported });

        await using var setup = await SetupAsync(librariesResult: Result<IPagedList<Library>>.Success(pagedList));

        setup.Cut.Find("#import-library .library-select-value").TextContent.Should().Be("Imported");
        await setup.ImportService.Received(1).GetImportJobsAsync(imported.Id, Arg.Any<CancellationToken>());
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
        await setup.Cut.ChooseLibraryAsync("import-library", setup.Library.Name);

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
        setup.Snackbar.Received().Add(Arg.Is<string>(s => s.Contains("uploaded")), Severity.Success,
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
        setup.GivenConfirmation(true);
        setup.ImportService.DeleteImportJobAsync(job.Id, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var card = setup.Cut.FindComponent<ImportJobCard>();
        await setup.Cut.InvokeAsync(() => card.Instance.OnDelete.InvokeAsync(job.Id));

        setup.Cut.Markup.Should().NotContain(job.OriginalFileName);
        await setup.DialogService.Received(1).ShowAsync<ConfirmationDialog>("Delete import",
            Arg.Is<DialogParameters>(p => ((string)p[nameof(ConfirmationDialog.ConfirmationMessage)]!).Contains(job.OriginalFileName)),
            Arg.Any<DialogOptions>());
    }

    [Fact]
    public async Task DeleteJobAsync_Should_KeepTheJob_WhenNotConfirmed()
    {
        var job = CreateJob(status: "Completed");
        await using var setup = await SetupAsync(initialJobs: [job]);
        setup.GivenConfirmation(false);

        var card = setup.Cut.FindComponent<ImportJobCard>();
        await setup.Cut.InvokeAsync(() => card.Instance.OnDelete.InvokeAsync(job.Id));

        await setup.ImportService.DidNotReceive().DeleteImportJobAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        setup.Cut.Markup.Should().Contain(job.OriginalFileName);
    }

    [Fact]
    public async Task DeleteJobAsync_Should_ShowSnackbarError_WhenFailed()
    {
        var job = CreateJob(status: "Completed");
        await using var setup = await SetupAsync(initialJobs: [job]);
        setup.GivenConfirmation(true);
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

        setup.Snackbar.Received().Add("Import marked as failed.", Severity.Warning,
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

    // ── DeleteCompletedJobsAsync ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteCompletedJobsAsync_Should_RemoveOnlyCompletedJobs()
    {
        var completedJob = CreateJob(status: "Completed", fileName: "done.cbz");
        var failedJob = CreateJob(status: "Failed", fileName: "failed.cbz");
        var activeJob = CreateJob(status: "Extracting", fileName: "active.cbz");
        await using var setup = await SetupAsync(initialJobs: [completedJob, failedJob, activeJob]);
        setup.GivenConfirmation(true);
        setup.ImportService.DeleteImportJobAsync(completedJob.Id, Arg.Any<CancellationToken>()).Returns(Result.Success());

        await setup.Cut.InvokeAsync(setup.Cut.Instance.DeleteCompletedJobsAsync);

        setup.Cut.Markup.Should().NotContain("done.cbz");
        setup.Cut.Markup.Should().Contain("failed.cbz").And.Contain("active.cbz");
        await setup.ImportService.DidNotReceive().DeleteImportJobAsync(failedJob.Id, Arg.Any<CancellationToken>());
        await setup.DialogService.Received(1).ShowAsync<ConfirmationDialog>("Clear successful imports",
            Arg.Is<DialogParameters>(p => ((string)p[nameof(ConfirmationDialog.ConfirmationMessage)]!)
                == "Delete 1 successful import(s) from the list? The imported books are kept. 2 import(s) running or failed will be kept."),
            Arg.Any<DialogOptions>());
        setup.Snackbar.Received().Add("1 import(s) deleted, 2 kept (running or failed).", Severity.Info,
            Arg.Any<Action<SnackbarOptions>?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task DeleteCompletedJobsAsync_Should_AlsoRemoveTheSplitArchives()
    {
        var splitJob = CreateJob(status: "Split", fileName: "volumes.rar");
        var activeJob = CreateJob(status: "Extracting", fileName: "active.cbz");
        await using var setup = await SetupAsync(initialJobs: [splitJob, activeJob]);
        setup.GivenConfirmation(true);
        setup.ImportService.DeleteImportJobAsync(splitJob.Id, Arg.Any<CancellationToken>()).Returns(Result.Success());

        await setup.Cut.InvokeAsync(setup.Cut.Instance.DeleteCompletedJobsAsync);

        setup.Cut.Markup.Should().NotContain("volumes.rar");
        setup.Cut.Markup.Should().Contain("active.cbz");
    }

    [Fact]
    public async Task DeleteCompletedJobsAsync_Should_DeleteNothing_WhenNotConfirmed()
    {
        var completedJob = CreateJob(status: "Completed", fileName: "done.cbz");
        await using var setup = await SetupAsync(initialJobs: [completedJob]);
        setup.GivenConfirmation(false);

        await setup.Cut.InvokeAsync(setup.Cut.Instance.DeleteCompletedJobsAsync);

        await setup.ImportService.DidNotReceive().DeleteImportJobAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        setup.Cut.Markup.Should().Contain("done.cbz");
    }

    [Fact]
    public async Task DeleteCompletedJobsAsync_Should_ReportSuccess_WhenOnlySuccessfulImportsAreListed()
    {
        var completedJob = CreateJob(status: "Completed", fileName: "done.cbz");
        await using var setup = await SetupAsync(initialJobs: [completedJob]);
        setup.GivenConfirmation(true);
        setup.ImportService.DeleteImportJobAsync(completedJob.Id, Arg.Any<CancellationToken>()).Returns(Result.Success());

        await setup.Cut.InvokeAsync(setup.Cut.Instance.DeleteCompletedJobsAsync);

        setup.Snackbar.Received().Add("1 import(s) deleted.", Severity.Success, Arg.Any<Action<SnackbarOptions>?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task DeleteCompletedJobsAsync_Should_ShowSnackbarError_WhenSomeDeletesFail()
    {
        var completedJob = CreateJob(status: "Completed", fileName: "done.cbz");
        await using var setup = await SetupAsync(initialJobs: [completedJob]);
        setup.GivenConfirmation(true);
        setup.ImportService.DeleteImportJobAsync(completedJob.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new TError("del:err", "boom")));

        await setup.Cut.InvokeAsync(setup.Cut.Instance.DeleteCompletedJobsAsync);

        setup.Snackbar.Received().Add("0 import(s) deleted, unable to delete 1.", Severity.Error,
            Arg.Any<Action<SnackbarOptions>?>(), Arg.Any<string?>());
    }

    // ── RetryJobAsync / AbandonJobAsync ───────────────────────────────────────

    [Fact]
    public async Task RetryJobAsync_Should_ShowTheJobAgainAsPending_WhenTheRetrySucceeds()
    {
        var failedJob = CreateJob(status: "Failed", fileName: "retry.cbz");
        await using var setup = await SetupAsync(initialJobs: [failedJob]);
        setup.ImportService.RetryImportJobAsync(failedJob.Id, Arg.Any<CancellationToken>()).Returns(Result.Success());
        setup.ImportService.GetImportJobsAsync(setup.Library.Id, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ImportJobViewModel>>.Success([failedJob with { Status = "Pending", StatusDisplay = "Waiting" }]));

        await setup.Cut.InvokeAsync(() => setup.Cut.Instance.RetryJobAsync(failedJob.Id));

        setup.Cut.Find(".import-job-status").TextContent.Should().Contain("Waiting");
        setup.Snackbar.Received().Add("Import restarted.", Severity.Success, Arg.Any<Action<SnackbarOptions>?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task RetryJobAsync_Should_ShowTheError_WhenTheRetryFails()
    {
        var failedJob = CreateJob(status: "Failed", fileName: "retry.cbz");
        await using var setup = await SetupAsync(initialJobs: [failedJob]);
        setup.ImportService.RetryImportJobAsync(failedJob.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new TError("IMP414", "file not found")));

        await setup.Cut.InvokeAsync(() => setup.Cut.Instance.RetryJobAsync(failedJob.Id));

        setup.Snackbar.Received().Add("file not found", Severity.Error, Arg.Any<Action<SnackbarOptions>?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task AbandonJobAsync_Should_RemoveTheJob_WhenConfirmed()
    {
        var failedJob = CreateJob(status: "Failed", fileName: "abandon.cbz");
        await using var setup = await SetupAsync(initialJobs: [failedJob]);
        setup.GivenConfirmation(true);
        setup.ImportService.AbandonImportJobAsync(failedJob.Id, Arg.Any<CancellationToken>()).Returns(Result.Success());

        await setup.Cut.InvokeAsync(() => setup.Cut.Instance.AbandonJobAsync(failedJob));

        setup.Cut.Markup.Should().NotContain("abandon.cbz");
    }

    [Fact]
    public async Task AbandonJobAsync_Should_DoNothing_WhenNotConfirmed()
    {
        var failedJob = CreateJob(status: "Failed", fileName: "abandon.cbz");
        await using var setup = await SetupAsync(initialJobs: [failedJob]);
        setup.GivenConfirmation(false);

        await setup.Cut.InvokeAsync(() => setup.Cut.Instance.AbandonJobAsync(failedJob));

        await setup.ImportService.DidNotReceive().AbandonImportJobAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        setup.Cut.Markup.Should().Contain("abandon.cbz");
    }

    [Fact]
    public async Task AbandonJobAsync_Should_KeepTheJobAndShowTheError_WhenTheAbandonFails()
    {
        var failedJob = CreateJob(status: "Failed", fileName: "abandon.cbz");
        await using var setup = await SetupAsync(initialJobs: [failedJob]);
        setup.GivenConfirmation(true);
        setup.ImportService.AbandonImportJobAsync(failedJob.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new TError("io", "cannot delete")));

        await setup.Cut.InvokeAsync(() => setup.Cut.Instance.AbandonJobAsync(failedJob));

        setup.Cut.Markup.Should().Contain("abandon.cbz");
        setup.Snackbar.Received().Add("cannot delete", Severity.Error, Arg.Any<Action<SnackbarOptions>?>(), Arg.Any<string?>());
    }

    // ── Polling ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task PollJobsAsync_Should_UpdateJobs_WhenSuccessful()
    {
        var job = CreateJob(status: "Extracting", fileName: "polling.cbz");
        await using var setup = await SetupAsync(initialJobs: [job]);

        var updatedJob = job with { Status = "Completed", StatusDisplay = "Completed" };
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
