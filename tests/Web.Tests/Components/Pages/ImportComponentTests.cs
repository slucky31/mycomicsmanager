using Application.Interfaces;
using AwesomeAssertions;
using Bunit;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages;
using Web.Models;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages;

public sealed class ImportComponentTests
{
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
                        (IReadOnlyList<ImportJobViewModel>)Array.Empty<ImportJobViewModel>()));
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
}
