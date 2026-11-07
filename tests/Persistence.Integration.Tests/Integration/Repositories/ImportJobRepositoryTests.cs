using Application.Interfaces;
using Base.Integration.Tests;
using Domain.ImportJobs;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence.Tests.Integration.Repositories;

[Collection("DatabaseCollectionTests")]
public class ImportJobRepositoryTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private IImportJobRepository Repository => _scope.ServiceProvider.GetRequiredService<IImportJobRepository>();

    private static ImportJob CreateJob(string fileName)
        => ImportJob.Create(fileName, $"/imports/{fileName}", 1_000, Guid.CreateVersion7()).Value!;

    [Fact]
    public async Task GetByIdsAsync_Should_ReturnOnlyRequestedJobs()
    {
        // Arrange
        var first = CreateJob("first.cbz");
        var second = CreateJob("second.cbz");
        var other = CreateJob("other.cbz");
        Repository.Add(first);
        Repository.Add(second);
        Repository.Add(other);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var jobs = await Repository.GetByIdsAsync([first.Id, second.Id, Guid.CreateVersion7()], TestContext.Current.CancellationToken);
        var none = await Repository.GetByIdsAsync([], TestContext.Current.CancellationToken);

        // Assert
        jobs.Select(j => j.Id).Should().BeEquivalentTo([first.Id, second.Id]);
        none.Should().BeEmpty();
    }

    [Fact]
    public async Task ExistsActiveForFilePathAsync_Should_IgnoreASplitArchive_WhoseArchivesAreKept()
    {
        // Arrange
        var split = CreateJob("volumes.rar");
        split.Advance(ImportJobStatus.Extracting);
        split.Split(["Serie - T01.cbz", "Serie - T02.cbz"]);
        Repository.Add(split);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var exists = await Repository.ExistsActiveForFilePathAsync(split.OriginalFilePath, TestContext.Current.CancellationToken);
        // The unique index on the file path only covers the imports still running: the same file can come again.
        Repository.Add(CreateJob("volumes.rar"));
        var saveAgain = await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();
        var reloaded = await Repository.GetByIdAsync(split.Id, TestContext.Current.CancellationToken);

        // Assert
        exists.Should().BeFalse();
        saveAgain.IsSuccess.Should().BeTrue();
        reloaded!.Status.Should().Be(ImportJobStatus.Split);
        reloaded.SplitFileNames.Should().Equal("Serie - T01.cbz", "Serie - T02.cbz");
    }
}
