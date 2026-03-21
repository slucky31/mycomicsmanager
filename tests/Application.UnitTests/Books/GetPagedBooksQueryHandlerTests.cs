using Application.Books.List;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.Books;

public class GetPagedBooksQueryHandlerTests
{
    private readonly GetPagedBooksQueryHandler _handler;
    private readonly IBookReadService _bookReadServiceMock;
    private readonly IRepository<Library, Guid> _libraryRepositoryMock;

    public GetPagedBooksQueryHandlerTests()
    {
        _bookReadServiceMock = Substitute.For<IBookReadService>();
        _libraryRepositoryMock = Substitute.For<IRepository<Library, Guid>>();
        _handler = new GetPagedBooksQueryHandler(_bookReadServiceMock, _libraryRepositoryMock);
    }

    private static Library CreateLibrary(Guid userId)
        => Library.Create("Test", "#FF0000", "book", LibraryBookType.Physical, userId).Value!;

    private static FakePagedList<Book> CreatePagedList(IReadOnlyCollection<Book> items, bool hasNextPage = false)
        => new(items, hasNextPage);

    private sealed class FakePagedList<T>(IReadOnlyCollection<T> items, bool hasNextPage) : IPagedList<T>
    {
        public IReadOnlyCollection<T>? Items { get; } = items;
        public bool HasNextPage { get; } = hasNextPage;
        public bool HasPreviousPage { get; } 
        public int Page { get; } = 1;
        public int PageSize { get; } = 24;
        public int TotalCount { get; } = items.Count;
        public Task<IPagedList<T>> ExecuteQueryAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult<IPagedList<T>>(this);
    }

    [Theory]
    [InlineData(0, 24)]
    [InlineData(-1, 24)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public async Task Handle_Should_ReturnBadRequest_WhenPageOrPageSizeIsInvalid(int page, int pageSize)
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var libraryId = Guid.CreateVersion7();
        var query = new GetPagedBooksQuery(userId, libraryId, page, pageSize, BookSortOrder.IdDesc);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
        await _libraryRepositoryMock.DidNotReceiveWithAnyArgs().GetByIdAsync(default);
        await _bookReadServiceMock.DidNotReceiveWithAnyArgs()
            .GetPagedByLibraryAsync(default, default, default, default, default, default);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenLibraryDoesNotExist()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var libraryId = Guid.CreateVersion7();
        var query = new GetPagedBooksQuery(userId, libraryId, 1, 24, BookSortOrder.IdDesc);

        _libraryRepositoryMock.GetByIdAsync(libraryId).Returns((Library?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
        await _bookReadServiceMock.DidNotReceiveWithAnyArgs()
            .GetPagedByLibraryAsync(default, default, default, default, default, default);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenLibraryBelongsToOtherUser()
    {
        // Arrange
        var ownerId = Guid.CreateVersion7();
        var requestingUserId = Guid.CreateVersion7();
        var libraryId = Guid.CreateVersion7();
        var query = new GetPagedBooksQuery(requestingUserId, libraryId, 1, 24, BookSortOrder.IdDesc);

        _libraryRepositoryMock.GetByIdAsync(libraryId).Returns(CreateLibrary(ownerId));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnPagedList_WhenLibraryOwnershipVerified()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var libraryId = Guid.CreateVersion7();
        var query = new GetPagedBooksQuery(userId, libraryId, 1, 24, BookSortOrder.IdDesc);

        var pagedList = CreatePagedList([]);
        _libraryRepositoryMock.GetByIdAsync(libraryId).Returns(CreateLibrary(userId));
        _bookReadServiceMock
            .GetPagedByLibraryAsync(libraryId, userId, 1, 24, BookSortOrder.IdDesc, null, CancellationToken.None)
            .Returns(Task.FromResult<IPagedList<Book>>(pagedList));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(pagedList);
    }

    [Fact]
    public async Task Handle_Should_ForwardSearchTermAndSortOrder_ToReadService()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var libraryId = Guid.CreateVersion7();
        var query = new GetPagedBooksQuery(userId, libraryId, 2, 24, BookSortOrder.SerieAndVolumeAsc, "naruto");

        _libraryRepositoryMock.GetByIdAsync(libraryId).Returns(CreateLibrary(userId));
        _bookReadServiceMock
            .GetPagedByLibraryAsync(libraryId, userId, 2, 24, BookSortOrder.SerieAndVolumeAsc, "naruto", CancellationToken.None)
            .Returns(Task.FromResult<IPagedList<Book>>(CreatePagedList([])));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _bookReadServiceMock.Received(1).GetPagedByLibraryAsync(
            libraryId, userId, 2, 24, BookSortOrder.SerieAndVolumeAsc, "naruto", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnHasNextPage_WhenMorePagesExist()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var libraryId = Guid.CreateVersion7();
        var query = new GetPagedBooksQuery(userId, libraryId, 1, 24, BookSortOrder.IdDesc);

        var pagedList = CreatePagedList([], hasNextPage: true);
        _libraryRepositoryMock.GetByIdAsync(libraryId).Returns(CreateLibrary(userId));
        _bookReadServiceMock
            .GetPagedByLibraryAsync(libraryId, userId, 1, 24, BookSortOrder.IdDesc, null, CancellationToken.None)
            .Returns(Task.FromResult<IPagedList<Book>>(pagedList));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.HasNextPage.Should().BeTrue();
    }
}
