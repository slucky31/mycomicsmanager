using AwesomeAssertions;
using Domain.Libraries;
using Web.Validators;
using Xunit;

namespace Web.Tests.Validators;

public sealed class LibraryUiDtoTests
{
    private static Library CreateLibrary(string name)
        => Library.Create(name, "#5C6BC0", "Bookmark", LibraryBookType.Physical, Guid.CreateVersion7()).Value!;

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var dto = new LibraryUiDto();

        // Assert
        dto.Name.Should().Be(string.Empty);
        dto.RelativePath.Should().Be(string.Empty);
        dto.Id.Should().Be(default(Guid));
        dto.CreatedOnUtc.Should().Be(default);
        dto.ModifiedOnUtc.Should().BeNull();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Name_ShouldAllowSettingValue()
    {
        // Arrange
        var dto = new LibraryUiDto();
        const string name = "Marvel Comics";

        // Act
        dto.Name = name;

        // Assert
        dto.Name.Should().Be(name);
    }

    [Fact]
    public void RelativePath_ShouldReturnUppercaseWithoutDiacritics()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = "Bibliothèque française"
        };

        // Act
        var relativePath = dto.RelativePath;

        // Assert
        relativePath.Should().Be("BIBLIOTHEQUE FRANCAISE");
    }

    [Fact]
    public void RelativePath_ShouldHandleEmptyName()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = string.Empty
        };

        // Act
        var relativePath = dto.RelativePath;

        // Assert
        relativePath.Should().Be(string.Empty);
    }

    [Fact]
    public void RelativePath_ShouldConvertToUppercase()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = "comic books"
        };

        // Act
        var relativePath = dto.RelativePath;

        // Assert
        relativePath.Should().Be("COMIC BOOKS");
    }

    [Fact]
    public void RelativePath_ShouldRemoveAccents()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = "éèêëÈÉÊË-ûüùÛÜÙ-ôöÔÖ-âàäÀÂÄ-îïÎÏ"
        };

        // Act
        var relativePath = dto.RelativePath;

        // Assert
        relativePath.Should().Be("EEEEEEEE-UUUUUU-OOOO-AAAAAA-IIII");
    }

    #endregion

    #region Convert Method Tests

    [Fact]
    public void Convert_ShouldMapAllProperties_WhenLibraryIsProvided()
    {
        // Arrange
        const string name = "DC Comics";
        var library = CreateLibrary(name);
        library.CreatedOnUtc = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        library.ModifiedOnUtc = new DateTime(2024, 1, 20, 14, 45, 0, DateTimeKind.Utc);

        // Act
        var dto = LibraryUiDto.Convert(library);

        // Assert
        dto.Id.Should().Be(library.Id);
        dto.Name.Should().Be(name);
        dto.CreatedOnUtc.Should().Be(library.CreatedOnUtc);
        dto.ModifiedOnUtc.Should().Be(library.ModifiedOnUtc);
        dto.DefaultBookSortOrder.Should().Be(library.DefaultBookSortOrder);
    }

    [Fact]
    public void Convert_Should_MapName_WhenNameHasSingleCharacter()
    {
        // Arrange
        var library = CreateLibrary("A");

        // Act
        var dto = LibraryUiDto.Convert(library);

        // Assert
        dto.Name.Should().Be("A");
    }

    [Fact]
    public void Convert_ShouldPreserveGuidId()
    {
        // Arrange
        var library = CreateLibrary("Image Comics");

        // Act
        var dto = LibraryUiDto.Convert(library);

        // Assert
        dto.Id.Should().NotBe(default(Guid));
        dto.Id.Should().Be(library.Id);
    }

    [Fact]
    public void Convert_ShouldHandleNullModifiedOnUtc()
    {
        // Arrange
        var library = CreateLibrary("Dark Horse Comics");

        // Act
        var dto = LibraryUiDto.Convert(library);

        // Assert
        dto.ModifiedOnUtc.Should().BeNull();
    }

    [Fact]
    public void Convert_ShouldHandleCompleteLibrary_WithAllFieldsPopulated()
    {
        // Arrange
        const string name = "Vertigo Comics";
        var library = CreateLibrary(name);
        var createdDate = new DateTime(2023, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var modifiedDate = new DateTime(2023, 6, 15, 16, 30, 0, DateTimeKind.Utc);
        library.CreatedOnUtc = createdDate;
        library.ModifiedOnUtc = modifiedDate;

        // Act
        var dto = LibraryUiDto.Convert(library);

        // Assert
        dto.Id.Should().Be(library.Id);
        dto.Name.Should().Be(name);
        dto.CreatedOnUtc.Should().Be(createdDate);
        dto.ModifiedOnUtc.Should().Be(modifiedDate);
    }

    [Fact]
    public void Convert_ShouldCreateNewInstance_EachTime()
    {
        // Arrange
        var library = CreateLibrary("IDW Publishing");

        // Act
        var dto1 = LibraryUiDto.Convert(library);
        var dto2 = LibraryUiDto.Convert(library);

        // Assert
        dto1.Should().NotBeSameAs(dto2);
        dto1.Id.Should().Be(dto2.Id);
    }

    [Fact]
    public void Convert_ShouldHandleLongStrings()
    {
        // Arrange
        var longName = new string('A', 100);
        var library = CreateLibrary(longName);

        // Act
        var dto = LibraryUiDto.Convert(library);

        // Assert
        dto.Name.Should().Be(longName);
        dto.Name.Length.Should().Be(100);
    }

    [Fact]
    public void Convert_ShouldHandleSpecialCharacters()
    {
        // Arrange
        const string name = "Bibliothèque française & européenne";
        var library = CreateLibrary(name);

        // Act
        var dto = LibraryUiDto.Convert(library);

        // Assert
        dto.Name.Should().Be(name);
    }

    [Fact]
    public void Convert_ShouldPreserveRelativePathCalculation()
    {
        // Arrange
        const string name = "Comic français";
        var library = CreateLibrary(name);

        // Act
        var dto = LibraryUiDto.Convert(library);

        // Assert
        dto.RelativePath.Should().Be("COMIC FRANCAIS");
        dto.RelativePath.Should().Be(library.RelativePath);
    }

    [Fact]
    public void Convert_ShouldHandleNumericCharacters()
    {
        // Arrange
        const string name = "Library 2024";
        var library = CreateLibrary(name);

        // Act
        var dto = LibraryUiDto.Convert(library);

        // Assert
        dto.Name.Should().Be(name);
        dto.RelativePath.Should().Be("LIBRARY 2024");
    }

    [Fact]
    public void Convert_ShouldHandleWhitespace()
    {
        // Arrange
        const string name = "  Multiple   Spaces  ";
        var library = CreateLibrary(name);

        // Act
        var dto = LibraryUiDto.Convert(library);

        // Assert
        dto.Name.Should().Be(name);
    }

    [Fact]
    public void RelativePath_ShouldMatchLibraryRelativePath()
    {
        // Arrange
        const string name = "Test Library éàù";
        var library = CreateLibrary(name);
        var dto = LibraryUiDto.Convert(library);

        // Act & Assert
        dto.RelativePath.Should().Be(library.RelativePath);
    }

    [Fact]
    public void Convert_ShouldHandleMixedCaseInput()
    {
        // Arrange
        const string name = "MiXeD CaSe LiBrArY";
        var library = CreateLibrary(name);

        // Act
        var dto = LibraryUiDto.Convert(library);

        // Assert
        dto.Name.Should().Be(name);
        dto.RelativePath.Should().Be("MIXED CASE LIBRARY");
    }

    #endregion
}
