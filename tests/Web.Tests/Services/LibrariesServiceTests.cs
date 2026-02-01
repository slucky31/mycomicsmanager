using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Libraries.Create;
using Application.Libraries.Delete;
using Application.Libraries.GetById;
using Application.Libraries.List;
using Application.Libraries.Update;
using Ardalis.GuardClauses;
using AwesomeAssertions;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class LibrariesServiceTests
{
    private readonly IQueryHandler<GetLibraryQuery, Library> _getLibraryHandler;
    private readonly IQueryHandler<GetLibrariesQuery, IPagedList<Library>> _getLibrariesHandler;
    private readonly ICommandHandler<CreateLibraryCommand, Library> _createLibraryHandler;
    private readonly ICommandHandler<UpdateLibraryCommand, Library> _updateLibraryHandler;
    private readonly ICommandHandler<DeleteLibraryCommand> _deleteLibraryHandler;
    private readonly LibrariesService _service;

    public LibrariesServiceTests()
    {
        _getLibraryHandler = Substitute.For<IQueryHandler<GetLibraryQuery, Library>>();
        _getLibrariesHandler = Substitute.For<IQueryHandler<GetLibrariesQuery, IPagedList<Library>>>();
        _createLibraryHandler = Substitute.For<ICommandHandler<CreateLibraryCommand, Library>>();
        _updateLibraryHandler = Substitute.For<ICommandHandler<UpdateLibraryCommand, Library>>();
        _deleteLibraryHandler = Substitute.For<ICommandHandler<DeleteLibraryCommand>>();

        _service = new LibrariesService(
            _getLibraryHandler,
            _getLibrariesHandler,
            _createLibraryHandler,
            _updateLibraryHandler,
            _deleteLibraryHandler);
    }

    #region GetById Tests

    [Fact]
    public async Task GetById_ShouldReturnValidationError_WhenIdIsNull()
    {
        // Arrange
        const string? id = null;

        // Act
        var result = await _service.GetById(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.ValidationError);
        await _getLibraryHandler.DidNotReceive().Handle(Arg.Any<GetLibraryQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_ShouldReturnValidationError_WhenIdIsEmpty()
    {
        // Arrange
        var id = string.Empty;

        // Act
        var result = await _service.GetById(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.ValidationError);
        await _getLibraryHandler.DidNotReceive().Handle(Arg.Any<GetLibraryQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_ShouldReturnValidationError_WhenIdIsInvalidGuid()
    {
        // Arrange
        const string id = "invalid-guid";

        // Act
        var result = await _service.GetById(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.ValidationError);
        await _getLibraryHandler.DidNotReceive().Handle(Arg.Any<GetLibraryQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_ShouldCallHandler_WhenIdIsValidGuid()
    {
        // Arrange
        var libraryId = Guid.CreateVersion7();
        var library = Library.Create("Test Library");
        _getLibraryHandler.Handle(Arg.Any<GetLibraryQuery>(), Arg.Any<CancellationToken>())
            .Returns(library);

        // Act
        var result = await _service.GetById(libraryId.ToString());

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _getLibraryHandler.Received(1).Handle(
            Arg.Is<GetLibraryQuery>(q => q.Id == libraryId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_ShouldReturnLibrary_WhenLibraryExists()
    {
        // Arrange
        var libraryId = Guid.CreateVersion7();
        var expectedLibrary = Library.Create("My Comics Library");
        _getLibraryHandler.Handle(Arg.Any<GetLibraryQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedLibrary);

        // Act
        var result = await _service.GetById(libraryId.ToString());

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedLibrary);
        result.Value.Name.Should().Be("My Comics Library");
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ShouldCallHandler_WhenNameIsProvided()
    {
        // Arrange
        const string name = "New Library";
        var library = Library.Create(name);
        _createLibraryHandler.Handle(Arg.Any<CreateLibraryCommand>(), Arg.Any<CancellationToken>())
            .Returns(library);

        // Act
        var result = await _service.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _createLibraryHandler.Received(1).Handle(
            Arg.Is<CreateLibraryCommand>(c => c.Name == name),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedLibrary()
    {
        // Arrange
        const string name = "Comics Collection";
        var expectedLibrary = Library.Create(name);
        _createLibraryHandler.Handle(Arg.Any<CreateLibraryCommand>(), Arg.Any<CancellationToken>())
            .Returns(expectedLibrary);

        // Act
        var result = await _service.Create(name);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedLibrary);
        result.Value.Name.Should().Be(name);
    }

    [Fact]
    public async Task Create_ShouldUseEmptyString_WhenNameIsNull()
    {
        // Arrange
        const string? name = null;
        var library = Library.Create("");
        _createLibraryHandler.Handle(Arg.Any<CreateLibraryCommand>(), Arg.Any<CancellationToken>())
            .Returns(library);

        // Act
        var result = await _service.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _createLibraryHandler.Received(1).Handle(
            Arg.Is<CreateLibraryCommand>(c => c.Name == ""),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_ShouldUseEmptyString_WhenNameIsEmpty()
    {
        // Arrange
        var name = string.Empty;
        var library = Library.Create("");
        _createLibraryHandler.Handle(Arg.Any<CreateLibraryCommand>(), Arg.Any<CancellationToken>())
            .Returns(library);

        // Act
        var result = await _service.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _createLibraryHandler.Received(1).Handle(
            Arg.Is<CreateLibraryCommand>(c => c.Name == ""),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldReturnValidationError_WhenIdIsNull()
    {
        // Arrange
        string? id = null;

        // Act
        var result = await _service.Update(id, "New Name");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.ValidationError);
        await _updateLibraryHandler.DidNotReceive().Handle(Arg.Any<UpdateLibraryCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ShouldReturnValidationError_WhenIdIsInvalidGuid()
    {
        // Arrange
        const string id = "not-a-guid";

        // Act
        var result = await _service.Update(id, "New Name");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.ValidationError);
        await _updateLibraryHandler.DidNotReceive().Handle(Arg.Any<UpdateLibraryCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ShouldCallHandler_WhenIdIsValidGuid()
    {
        // Arrange
        var libraryId = Guid.CreateVersion7();
        const string name = "Updated Library Name";
        var library = Library.Create(name);
        _updateLibraryHandler.Handle(Arg.Any<UpdateLibraryCommand>(), Arg.Any<CancellationToken>())
            .Returns(library);

        // Act
        var result = await _service.Update(libraryId.ToString(), name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _updateLibraryHandler.Received(1).Handle(
            Arg.Is<UpdateLibraryCommand>(c =>
                c.Id == libraryId &&
                c.Name == name),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ShouldReturnUpdatedLibrary()
    {
        // Arrange
        var libraryId = Guid.CreateVersion7();
        const string name = "Renamed Library";
        var expectedLibrary = Library.Create(name);
        _updateLibraryHandler.Handle(Arg.Any<UpdateLibraryCommand>(), Arg.Any<CancellationToken>())
            .Returns(expectedLibrary);

        // Act
        var result = await _service.Update(libraryId.ToString(), name);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedLibrary);
        result.Value.Name.Should().Be(name);
    }

    [Fact]
    public async Task Update_ShouldUseEmptyString_WhenNameIsNull()
    {
        // Arrange
        var libraryId = Guid.CreateVersion7();
        string? name = null;
        var library = Library.Create("");
        _updateLibraryHandler.Handle(Arg.Any<UpdateLibraryCommand>(), Arg.Any<CancellationToken>())
            .Returns(library);

        // Act
        var result = await _service.Update(libraryId.ToString(), name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _updateLibraryHandler.Received(1).Handle(
            Arg.Is<UpdateLibraryCommand>(c => c.Name == ""),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region FilterBy Tests

    [Fact]
    public async Task FilterBy_ShouldCallHandler_WithProvidedParameters()
    {
        // Arrange
        const string searchTerm = "comics";
        const LibrariesColumn sortColumn = LibrariesColumn.Name;
        const SortOrder sortOrder = SortOrder.Ascending;
        const int page = 1;
        const int pageSize = 10;
        var pagedList = Substitute.For<IPagedList<Library>>();
        _getLibrariesHandler.Handle(Arg.Any<GetLibrariesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IPagedList<Library>>.Success(pagedList));

        // Act
        var result = await _service.FilterBy(searchTerm, sortColumn, sortOrder, page, pageSize);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _getLibrariesHandler.Received(1).Handle(
            Arg.Is<GetLibrariesQuery>(q =>
                q.searchTerm == searchTerm &&
                q.sortColumn == sortColumn &&
                q.sortOrder == sortOrder &&
                q.page == page &&
                q.pageSize == pageSize),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterBy_ShouldReturnPagedList()
    {
        // Arrange
        var pagedList = Substitute.For<IPagedList<Library>>();
        pagedList.Items.Returns([
            Library.Create("Library 1"),
            Library.Create("Library 2")
        ]);
        pagedList.TotalCount.Returns(2);
        _getLibrariesHandler.Handle(Arg.Any<GetLibrariesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IPagedList<Library>>.Success(pagedList));

        // Act
        var result = await _service.FilterBy("test", LibrariesColumn.Name, SortOrder.Ascending, 1, 10);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(pagedList);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task FilterBy_ShouldHandleNullSearchTerm()
    {
        // Arrange
        const string? searchTerm = null;
        var pagedList = Substitute.For<IPagedList<Library>>();
        _getLibrariesHandler.Handle(Arg.Any<GetLibrariesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IPagedList<Library>>.Success(pagedList));

        // Act
        var result = await _service.FilterBy(searchTerm, LibrariesColumn.Name, SortOrder.Ascending, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _getLibrariesHandler.Received(1).Handle(
            Arg.Is<GetLibrariesQuery>(q => q.searchTerm == searchTerm),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterBy_ShouldHandleNullSortColumn()
    {
        // Arrange
        LibrariesColumn? sortColumn = null;
        var pagedList = Substitute.For<IPagedList<Library>>();
        _getLibrariesHandler.Handle(Arg.Any<GetLibrariesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IPagedList<Library>>.Success(pagedList));

        // Act
        var result = await _service.FilterBy("test", sortColumn, SortOrder.Ascending, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _getLibrariesHandler.Received(1).Handle(
            Arg.Is<GetLibrariesQuery>(q => q.sortColumn == sortColumn),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterBy_ShouldHandleNullSortOrder()
    {
        // Arrange
        SortOrder? sortOrder = null;
        var pagedList = Substitute.For<IPagedList<Library>>();
        _getLibrariesHandler.Handle(Arg.Any<GetLibrariesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IPagedList<Library>>.Success(pagedList));

        // Act
        var result = await _service.FilterBy("test", LibrariesColumn.Name, sortOrder, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _getLibrariesHandler.Received(1).Handle(
            Arg.Is<GetLibrariesQuery>(q => q.sortOrder == sortOrder),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterBy_ShouldHandleDescendingSortOrder()
    {
        // Arrange
        var pagedList = Substitute.For<IPagedList<Library>>();
        _getLibrariesHandler.Handle(Arg.Any<GetLibrariesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IPagedList<Library>>.Success(pagedList));

        // Act
        var result = await _service.FilterBy("test", LibrariesColumn.Name, SortOrder.Descending, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _getLibrariesHandler.Received(1).Handle(
            Arg.Is<GetLibrariesQuery>(q => q.sortOrder == SortOrder.Descending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterBy_ShouldHandleDifferentPageSizes()
    {
        // Arrange
        const int pageSize = 25;
        var pagedList = Substitute.For<IPagedList<Library>>();
        _getLibrariesHandler.Handle(Arg.Any<GetLibrariesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IPagedList<Library>>.Success(pagedList));

        // Act
        var result = await _service.FilterBy("test", LibrariesColumn.Name, SortOrder.Ascending, 1, pageSize);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _getLibrariesHandler.Received(1).Handle(
            Arg.Is<GetLibrariesQuery>(q => q.pageSize == pageSize),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterBy_ShouldHandleDifferentPages()
    {
        // Arrange
        const int page = 3;
        var pagedList = Substitute.For<IPagedList<Library>>();
        _getLibrariesHandler.Handle(Arg.Any<GetLibrariesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IPagedList<Library>>.Success(pagedList));

        // Act
        var result = await _service.FilterBy("test", LibrariesColumn.Name, SortOrder.Ascending, page, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _getLibrariesHandler.Received(1).Handle(
            Arg.Is<GetLibrariesQuery>(q => q.page == page),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldReturnValidationError_WhenIdIsNull()
    {
        // Arrange
        string? id = null;

        // Act
        var result = await _service.Delete(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.ValidationError);
        await _deleteLibraryHandler.DidNotReceive().Handle(Arg.Any<DeleteLibraryCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ShouldReturnValidationError_WhenIdIsEmpty()
    {
        // Arrange
        var id = string.Empty;

        // Act
        var result = await _service.Delete(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.ValidationError);
        await _deleteLibraryHandler.DidNotReceive().Handle(Arg.Any<DeleteLibraryCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ShouldReturnValidationError_WhenIdIsInvalidGuid()
    {
        // Arrange
        const string id = "invalid-guid-format";

        // Act
        var result = await _service.Delete(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.ValidationError);
        await _deleteLibraryHandler.DidNotReceive().Handle(Arg.Any<DeleteLibraryCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ShouldCallHandler_WhenIdIsValidGuid()
    {
        // Arrange
        var libraryId = Guid.CreateVersion7();
        _deleteLibraryHandler.Handle(Arg.Any<DeleteLibraryCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _service.Delete(libraryId.ToString());

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _deleteLibraryHandler.Received(1).Handle(
            Arg.Is<DeleteLibraryCommand>(c => c.Id == libraryId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenDeletionSucceeds()
    {
        // Arrange
        var libraryId = Guid.CreateVersion7();
        _deleteLibraryHandler.Handle(Arg.Any<DeleteLibraryCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _service.Delete(libraryId.ToString());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenHandlerReturnsFailure()
    {
        // Arrange
        var libraryId = Guid.CreateVersion7();
        var expectedError = LibrariesError.NotFound;
        _deleteLibraryHandler.Handle(Arg.Any<DeleteLibraryCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(expectedError));

        // Act
        var result = await _service.Delete(libraryId.ToString());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(expectedError);
    }

    #endregion
}
