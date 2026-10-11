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
}
