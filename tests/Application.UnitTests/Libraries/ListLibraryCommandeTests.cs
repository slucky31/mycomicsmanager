using Application.Libraries;
using Application.Libraries.List;
using Ardalis.GuardClauses;
using Domain.Extensions;
using Domain.Libraries;
using MockQueryable.EntityFrameworkCore;
using NSubstitute;
using Persistence.Queries.Helpers;

namespace Application.UnitTests.Libraries;
public class ListLibraryCommandeTests
{
    private static readonly GetLibrariesQuery request = new(null, null, null, 1, 10);
    private static readonly Library library = Library.Create("library");


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
        List<Library> list = [library];
        var query = list.AsQueryable().BuildMock();
        var mockPagedList = new PagedList<Library>(query);
        _libraryReadServiceMock.GetLibrariesAsync(request.searchTerm, request.sortColumn, request.sortOrder, request.page, request.pageSize).Returns(mockPagedList);

        // Act
        var pagedList = await _handler.Handle(request, default);
        await pagedList.ExecuteQueryAsync(request.page, request.pageSize);

        // Assert
        Guard.Against.Null(pagedList);
        pagedList.TotalCount.Should().Be(1);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.First().Name.Should().Be("library");
        pagedList.Items.First().RelativePath.Should().Be("library".RemoveDiacritics().ToUpperInvariant());
    }
}
