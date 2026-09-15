using Base.Integration.Tests;

namespace Persistence.Tests.Integration.Repositories;

[Collection("DatabaseCollectionTests")]
public sealed class IsbnBedethequeCacheRepositoryTests(IntegrationTestWebAppFactory factory)
    : IsbnBedethequeCacheIntegrationTest(factory)
{
    private const string TestIsbn = "9782205057317";
    private const string TestUrl = "https://www.bedetheque.com/BD-Biguden-Tome-1-LAnkou-224335.html";

    [Fact]
    public async Task GetUrlByIsbnAsync_ShouldReturnNull_WhenIsbnNotCached()
    {
        // Act
        var result = await CacheRepository.GetUrlByIsbnAsync("9780000000000", TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_ThenGetUrlByIsbnAsync_ShouldReturnSavedUrl()
    {
        // Arrange
        var isbn = UniqueIsbn();
        var url = "https://www.bedetheque.com/BD-Test-224335.html";

        // Act
        await CacheRepository.SaveAsync(isbn, url, TestContext.Current.CancellationToken);
        var result = await CacheRepository.GetUrlByIsbnAsync(isbn, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(url);
    }

    [Fact]
    public async Task GetUrlByIsbnAsync_ShouldReturnNull_WhenDifferentIsbnQueried()
    {
        // Arrange
        var isbn = UniqueIsbn();
        await CacheRepository.SaveAsync(isbn, TestUrl, TestContext.Current.CancellationToken);

        // Act
        var result = await CacheRepository.GetUrlByIsbnAsync("9780000000001", TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_ShouldNotThrow_WhenSamePairSavedTwice()
    {
        // Arrange
        var isbn = UniqueIsbn();

        // Act
        await CacheRepository.SaveAsync(isbn, TestUrl, TestContext.Current.CancellationToken);
        var action = async () => await CacheRepository.SaveAsync(isbn, TestUrl);

        // Assert — duplicate key must be silently ignored
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetUrlByIsbnAsync_ShouldReturnFirstUrl_WhenDuplicateSaveAttempted()
    {
        // Arrange
        var isbn = UniqueIsbn();
        var firstUrl = "https://www.bedetheque.com/BD-First-111111.html";
        var secondUrl = "https://www.bedetheque.com/BD-Second-222222.html";

        // Act
        await CacheRepository.SaveAsync(isbn, firstUrl, TestContext.Current.CancellationToken);
        await CacheRepository.SaveAsync(isbn, secondUrl, TestContext.Current.CancellationToken); // duplicate — should be silently ignored

        var result = await CacheRepository.GetUrlByIsbnAsync(isbn, TestContext.Current.CancellationToken);

        // Assert — first saved URL is preserved
        result.Should().Be(firstUrl);
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistMultipleDistinctIsbns()
    {
        // Arrange
        var isbn1 = UniqueIsbn();
        var isbn2 = UniqueIsbn();
        var url1 = "https://www.bedetheque.com/BD-SerieA-100001.html";
        var url2 = "https://www.bedetheque.com/BD-SerieB-100002.html";

        // Act
        await CacheRepository.SaveAsync(isbn1, url1, TestContext.Current.CancellationToken);
        await CacheRepository.SaveAsync(isbn2, url2, TestContext.Current.CancellationToken);

        // Assert
        (await CacheRepository.GetUrlByIsbnAsync(isbn1, TestContext.Current.CancellationToken)).Should().Be(url1);
        (await CacheRepository.GetUrlByIsbnAsync(isbn2, TestContext.Current.CancellationToken)).Should().Be(url2);
    }

    [Fact]
    public async Task SaveAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var action = async () => await CacheRepository.SaveAsync(UniqueIsbn(), TestUrl, cts.Token);

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetUrlByIsbnAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var action = async () => await CacheRepository.GetUrlByIsbnAsync(TestIsbn, cts.Token);

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    // Each test gets its own unique ISBN to stay isolated inside the shared transaction.
    private static string UniqueIsbn() => $"978{Guid.NewGuid().ToString("N")[..10]}";
}
