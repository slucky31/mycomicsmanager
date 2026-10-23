using Application.Series;
using Base.Integration.Tests;
using Domain.Series;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence.Tests.Integration.Repositories;

[Collection("DatabaseCollectionTests")]
public class SeriesInfoRepositoryTests(IntegrationTestWebAppFactory factory) : BookIntegrationTest(factory)
{
    private ISeriesInfoRepository Repository => _scope.ServiceProvider.GetRequiredService<ISeriesInfoRepository>();

    [Fact]
    public async Task GetAsync_Should_FindTheSeriesByItsNormalizedName_InItsLibrary()
    {
        // Arrange
        Repository.Add(SeriesInfo.Create(DefaultLibrary.Id, "Thorgal", 41, isFinished: false).Value!);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        // Act
        var stored = await Repository.GetAsync(DefaultLibrary.Id, "THORGAL", TestContext.Current.CancellationToken);
        var elsewhere = await Repository.GetAsync(Guid.CreateVersion7(), "THORGAL", TestContext.Current.CancellationToken);

        // Assert
        stored.Should().NotBeNull();
        stored!.TotalVolumes.Should().Be(41);
        elsewhere.Should().BeNull();
    }
}
