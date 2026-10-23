using Application.Interfaces;
using Application.Series;
using Base.Integration.Tests;
using Domain.Books;
using Domain.Libraries;
using Domain.Series;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence.Tests.Integration.Queries;

[Collection("DatabaseCollectionTests")]
public class SeriesReadServiceTests(IntegrationTestWebAppFactory factory) : BookIntegrationTest(factory)
{
    private ISeriesReadService ReadService => _scope.ServiceProvider.GetRequiredService<ISeriesReadService>();

    private ISeriesInfoRepository InfoRepository => _scope.ServiceProvider.GetRequiredService<ISeriesInfoRepository>();

    private async Task<Library> AddLibraryAsync(string name, Guid userId)
    {
        var library = Library.Create(name, "#000000", "book", LibraryBookType.Physical, userId).Value!;
        _scope.ServiceProvider.GetRequiredService<IRepository<Library, Guid>>().Add(library);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
        return library;
    }

    private async Task<PhysicalBook> AddBookAsync(Guid libraryId, string serie, int volume)
    {
        var book = PhysicalBook.Create(new BookMetadata(serie, $"{serie} {volume}", $"97800000{volume:D5}", volume), libraryId).Value!;
        BookRepository.Add(book);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
        return book;
    }

    [Fact]
    public async Task ListBooksAsync_Should_ReturnTheBooksOfTheUser_ByVolume_OrOfOneLibrary()
    {
        // Arrange
        var other = await AddLibraryAsync("Other", DefaultLibrary.UserId);
        var third = await AddBookAsync(DefaultLibrary.Id, "Thorgal", 3);
        var first = await AddBookAsync(DefaultLibrary.Id, "Thorgal", 1);
        var elsewhere = await AddBookAsync(other.Id, "Blacksad", 2);
        var stranger = await AddLibraryAsync("Stranger", Guid.CreateVersion7());
        await AddBookAsync(stranger.Id, "Thorgal", 2);

        // Act
        var all = await ReadService.ListBooksAsync(DefaultLibrary.UserId, null, TestContext.Current.CancellationToken);
        var one = await ReadService.ListBooksAsync(DefaultLibrary.UserId, DefaultLibrary.Id, TestContext.Current.CancellationToken);

        // Assert
        all.Select(b => b.BookId).Should().Equal(first.Id, elsewhere.Id, third.Id);
        one.Select(b => b.BookId).Should().Equal(first.Id, third.Id);
        one[0].Should().Be(new SerieBookRow(first.Id, DefaultLibrary.Id, "Default", "Thorgal", "Thorgal 1", 1, string.Empty));
    }

    [Fact]
    public async Task ListInfosAsync_Should_ReturnTheInfosOfTheUser_OrOfOneLibrary()
    {
        // Arrange
        var other = await AddLibraryAsync("Other", DefaultLibrary.UserId);
        var stranger = await AddLibraryAsync("Stranger", Guid.CreateVersion7());
        InfoRepository.Add(SeriesInfo.Create(DefaultLibrary.Id, "Thorgal", 41, isFinished: false).Value!);
        InfoRepository.Add(SeriesInfo.Create(other.Id, "Blacksad", 7, isFinished: false).Value!);
        InfoRepository.Add(SeriesInfo.Create(stranger.Id, "XIII", 20, isFinished: true).Value!);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var all = await ReadService.ListInfosAsync(DefaultLibrary.UserId, null, TestContext.Current.CancellationToken);
        var one = await ReadService.ListInfosAsync(DefaultLibrary.UserId, DefaultLibrary.Id, TestContext.Current.CancellationToken);

        // Assert
        all.Select(i => i.Name).Should().BeEquivalentTo("Thorgal", "Blacksad");
        one.Select(i => i.Name).Should().Equal("Thorgal");
    }
}
