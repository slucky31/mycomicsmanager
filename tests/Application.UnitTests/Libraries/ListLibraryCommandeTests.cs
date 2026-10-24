using Application.Interfaces;
using Application.Libraries;
using Application.Libraries.List;
using Ardalis.GuardClauses;
using Domain.Extensions;
using Domain.Libraries;
using Domain.Users;
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
    private readonly IRepository<User, Guid> _userRepository = Substitute.For<IRepository<User, Guid>>();

    public ListLibraryCommandeTests()
    {
        _libraryReadServiceMock = Substitute.For<ILibraryReadService>();

        _handler = new GetLibrariesQueryHandler(_libraryReadServiceMock, _userRepository);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess()
    {
        // Arrange
        List<Library> list = [s_library];
        var query = list.BuildMock();
        var mockPagedList = new PagedList<Library>(query);
        _libraryReadServiceMock.GetLibrariesAsync(s_request.searchTerm, s_request.sortColumn, s_request.sortOrder, s_request.page, s_request.pageSize, new UserLibraries(s_request.UserId, false), Arg.Any<CancellationToken>()).Returns(mockPagedList);

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

    [Fact]
    public async Task Handle_Should_ListTheHiddenLibraries_WhenTheUserShowsThem()
    {
        // Arrange
        var user = User.Create("user@example.com", "auth-id");
        user.UpdateShowHiddenLibraries(true);
        _userRepository.GetByIdAsync(s_request.UserId).Returns(user);

        // Act
        await _handler.Handle(s_request, TestContext.Current.CancellationToken);

        // Assert
        await _libraryReadServiceMock.Received(1).GetLibrariesAsync(s_request.searchTerm, s_request.sortColumn, s_request.sortOrder, s_request.page, s_request.pageSize, new UserLibraries(s_request.UserId, true), Arg.Any<CancellationToken>());
    }
}
