using Application.Interfaces;
using Base.Integration.Tests;
using Domain.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence.Tests.Integration.Repositories;

[Collection("DatabaseCollectionTests")]
public class FeatureToggleOverrideRepositoryTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private IFeatureToggleOverrideRepository Repository => _scope.ServiceProvider.GetRequiredService<IFeatureToggleOverrideRepository>();

    [Fact]
    public async Task GetAsync_Should_RoundTripTheOverride_WhenItWasAddedAndUpdated()
    {
        // Arrange
        Repository.Add(FeatureToggleOverride.Create(FeatureToggle.Bedetheque, true));
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        // Act
        var stored = await Repository.GetAsync(FeatureToggle.Bedetheque, TestContext.Current.CancellationToken);
        stored!.Set(false);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();
        var updated = await Repository.GetAsync(FeatureToggle.Bedetheque, TestContext.Current.CancellationToken);

        // Assert
        updated.Should().NotBeNull();
        updated.Enabled.Should().BeFalse();
        updated.ModifiedOnUtc.Should().NotBeNull();
        (await Repository.GetAsync(FeatureToggle.IsbnOcr, TestContext.Current.CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_Should_ListTheOverrides_AndRemoveShouldDeleteOne()
    {
        // Arrange
        var feedImport = FeatureToggleOverride.Create(FeatureToggle.FeedImport, true);
        Repository.Add(feedImport);
        Repository.Add(FeatureToggleOverride.Create(FeatureToggle.IsbnOcr, false));
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var all = await Repository.GetAllAsync(TestContext.Current.CancellationToken);
        Repository.Remove(feedImport);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        var remaining = await Repository.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        all.Select(o => o.Toggle).Should().BeEquivalentTo([FeatureToggle.FeedImport, FeatureToggle.IsbnOcr]);
        remaining.Should().ContainSingle().Which.Toggle.Should().Be(FeatureToggle.IsbnOcr);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Fail_WhenTheSameFeatureIsOverriddenTwice()
    {
        // Arrange
        Repository.Add(FeatureToggleOverride.Create(FeatureToggle.FeedImport, true));
        Repository.Add(FeatureToggleOverride.Create(FeatureToggle.FeedImport, false));

        // Act
        var result = await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
