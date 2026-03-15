using Domain.Books;
using Domain.Libraries;

namespace Domain.UnitTests.Libraries;

public class LibraryTests
{
    private static readonly Guid DefaultUserId = Guid.CreateVersion7();

    // -------------------------------------------------------
    // Create
    // -------------------------------------------------------

    [Fact]
    public void Create_Should_CreateLibrary_WhenAllParametersValid()
    {
        // Arrange
        const string name = "My Manga";
        const string color = "#7B5EA7";
        const string icon = "AutoStories";

        // Act
        var result = Library.Create(name, color, icon, LibraryBookType.Physical, DefaultUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var library = result.Value!;
        library.Name.Should().Be(name);
        library.Color.Should().Be(color);
        library.Icon.Should().Be(icon);
        library.BookType.Should().Be(LibraryBookType.Physical);
        library.UserId.Should().Be(DefaultUserId);
        library.Id.Should().NotBe(Guid.Empty);
        library.DefaultBookSortOrder.Should().Be(BookSortOrder.IdDesc);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenBookTypeIsUndefined()
    {
        // Act
        var result = Library.Create("My Library", "#5C6BC0", "Book", (LibraryBookType)999, DefaultUserId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenNameIsEmpty()
    {
        // Act
        var result = Library.Create("", "#5C6BC0", "Book", LibraryBookType.Physical, DefaultUserId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenNameExceedsMaxLength()
    {
        // Arrange
        var longName = new string('A', LibraryConstants.MaxNameLength + 1);

        // Act
        var result = Library.Create(longName, "#5C6BC0", "Book", LibraryBookType.Physical, DefaultUserId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenColorIsEmpty()
    {
        // Act
        var result = Library.Create("My Library", "", "Book", LibraryBookType.Physical, DefaultUserId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenColorExceedsMaxLength()
    {
        // Arrange — 8 chars, exceeds max of 7
        var longColor = "#5C6BC0X";

        // Act
        var result = Library.Create("My Library", longColor, "Book", LibraryBookType.Physical, DefaultUserId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenIconIsEmpty()
    {
        // Act
        var result = Library.Create("My Library", "#5C6BC0", "", LibraryBookType.Physical, DefaultUserId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenUserIdIsEmpty()
    {
        // Act
        var result = Library.Create("My Library", "#5C6BC0", "Book", LibraryBookType.Physical, Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BadRequest);
    }

    [Fact]
    public void Create_Should_HaveComputedRelativePath()
    {
        // Act
        var result = Library.Create("Mes BDs", "#5C6BC0", "Book", LibraryBookType.Digital, DefaultUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RelativePath.Should().Be("MES BDS");
    }

    // -------------------------------------------------------
    // Update (color + icon)
    // -------------------------------------------------------

    [Fact]
    public void Update_Should_UpdateColorAndIcon_WhenValid()
    {
        // Arrange
        var library = Library.Create("My Library", "#5C6BC0", "Bookmark", LibraryBookType.Physical, DefaultUserId).Value!;

        // Act
        var result = library.Update("#FF7043", "MenuBook");

        // Assert
        result.IsSuccess.Should().BeTrue();
        library.Color.Should().Be("#FF7043");
        library.Icon.Should().Be("MenuBook");
    }

    [Fact]
    public void Update_Should_ReturnBadRequest_WhenColorIsEmpty()
    {
        // Arrange
        var library = Library.Create("My Library", "#5C6BC0", "Bookmark", LibraryBookType.Physical, DefaultUserId).Value!;

        // Act
        var result = library.Update("", "MenuBook");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BadRequest);
    }

    [Fact]
    public void Update_Should_ReturnBadRequest_WhenIconIsEmpty()
    {
        // Arrange
        var library = Library.Create("My Library", "#5C6BC0", "Bookmark", LibraryBookType.Physical, DefaultUserId).Value!;

        // Act
        var result = library.Update("#5C6BC0", "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BadRequest);
    }

    // -------------------------------------------------------
    // UpdateName
    // -------------------------------------------------------

    [Fact]
    public void UpdateName_Should_UpdateName_WhenNotDefaultLibrary()
    {
        // Arrange
        var library = Library.Create("Old Name", "#5C6BC0", "Book", LibraryBookType.Physical, DefaultUserId).Value!;

        // Act
        var result = library.UpdateName("New Name");

        // Assert
        result.IsSuccess.Should().BeTrue();
        library.Name.Should().Be("New Name");
    }

    [Fact]
    public void UpdateName_Should_ReturnBadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var library = Library.Create("My Library", "#5C6BC0", "Book", LibraryBookType.Physical, DefaultUserId).Value!;

        // Act
        var result = library.UpdateName("");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BadRequest);
    }

    // -------------------------------------------------------
    // UpdateDefaultBookSortOrder
    // -------------------------------------------------------

    [Fact]
    public void UpdateDefaultBookSortOrder_Should_UpdateSortOrder_WhenValid()
    {
        // Arrange
        var library = Library.Create("My Library", "#5C6BC0", "Bookmark", LibraryBookType.Physical, DefaultUserId).Value!;

        // Act
        var result = library.UpdateDefaultBookSortOrder(BookSortOrder.SerieAndVolumeAsc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        library.DefaultBookSortOrder.Should().Be(BookSortOrder.SerieAndVolumeAsc);
    }

    [Fact]
    public void UpdateDefaultBookSortOrder_Should_ReturnBadRequest_WhenSortOrderIsUndefined()
    {
        // Arrange
        var library = Library.Create("My Library", "#5C6BC0", "Bookmark", LibraryBookType.Physical, DefaultUserId).Value!;

        // Act
        var result = library.UpdateDefaultBookSortOrder((BookSortOrder)999);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BadRequest);
    }

    // -------------------------------------------------------
    // CanContain
    // -------------------------------------------------------

    [Fact]
    public void CanContain_Should_ReturnSuccess_WhenPhysicalLibraryContainsPhysicalBook()
    {
        // Arrange
        var library = Library.Create("Comics", "#5C6BC0", "Book", LibraryBookType.Physical, DefaultUserId).Value!;
        var physicalBook = PhysicalBook.Create("Series", "Title", "isbn", libraryId: library.Id).Value!;

        // Act
        var result = library.CanContain(physicalBook);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CanContain_Should_ReturnBookTypeMismatch_WhenPhysicalLibraryContainsDigitalBook()
    {
        // Arrange
        var library = Library.Create("Comics", "#5C6BC0", "Book", LibraryBookType.Physical, DefaultUserId).Value!;
        var digitalBook = (DigitalBook)Activator.CreateInstance(typeof(DigitalBook), nonPublic: true)!;

        // Act
        var result = library.CanContain(digitalBook);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BookTypeMismatch);
    }

    [Fact]
    public void CanContain_Should_ReturnSuccess_WhenDigitalLibraryContainsDigitalBook()
    {
        // Arrange
        var library = Library.Create("eBooks", "#5C6BC0", "Book", LibraryBookType.Digital, DefaultUserId).Value!;
        var digitalBook = (DigitalBook)Activator.CreateInstance(typeof(DigitalBook), nonPublic: true)!;

        // Act
        var result = library.CanContain(digitalBook);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CanContain_Should_ReturnBookTypeMismatch_WhenDigitalLibraryContainsPhysicalBook()
    {
        // Arrange
        var library = Library.Create("eBooks", "#5C6BC0", "Book", LibraryBookType.Digital, DefaultUserId).Value!;
        var physicalBook = PhysicalBook.Create("Series", "Title", "isbn", libraryId: library.Id).Value!;

        // Act
        var result = library.CanContain(physicalBook);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BookTypeMismatch);
    }

    [Fact]
    public void Books_Should_ReturnEmptyReadOnlyList_WhenLibraryIsCreated()
    {
        // Act
        var library = Library.Create("My Library", "#5C6BC0", "Book", LibraryBookType.Physical, DefaultUserId).Value!;

        // Assert
        library.Books.Should().BeEmpty();
        library.Books.Should().BeAssignableTo<IReadOnlyList<Book>>();
    }
}
