using Application.Libraries;
using Application.Libraries.List;
using Ardalis.GuardClauses;
using Domain.Extensions;
using Domain.Libraries;
using MockQueryable;
using NSubstitute;
using Persistence.Queries.Helpers;

namespace Application.UnitTests.Libraries;

public class ListLibraryCommandeTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly GetLibrariesQuery s_request = new(null, null, null, 1, 10, s_userId);
    private static readonly Library s_library = Library.Create("library", "#5C6BC0", "Bookmark", LibraryBookType.Physical, s_userId).Value!;

    private readonly GetLibrariesQueryHandler _handler;
    private readonly ILibraryReadService _libraryReadServiceMock;

    public ListLibraryCommandeTests()
    {
        _libraryReadServiceMock = Substitute.For<ILibraryReadService>();

        _handler = new GetLibrariesQueryHandler(_libraryReadServiceMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess()
    {
        // Arrange
        List<Library> list = [s_library];
        var query = list.BuildMock();
        var mockPagedList = new PagedList<Library>(query);
        _libraryReadServiceMock.GetLibrariesAsync(s_request.searchTerm, s_request.sortColumn, s_request.sortOrder, s_request.page, s_request.pageSize, s_request.UserId, Arg.Any<CancellationToken>()).Returns(mockPagedList);

        // Act
        var result = await _handler.Handle(s_request, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        result.IsSuccess.Should().BeTrue();
        var pagedList = result.Value;
        Assert.NotNull(pagedList);
        await pagedList.ExecuteQueryAsync(s_request.page, s_request.pageSize, TestContext.Current.CancellationToken);

        // Assert
        Guard.Against.Null(pagedList);
        pagedList.TotalCount.Should().Be(1);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.First().Name.Should().Be("library");
        pagedList.Items.First().RelativePath.Should().Be("library".RemoveDiacritics().ToUpperInvariant());
    }
}
