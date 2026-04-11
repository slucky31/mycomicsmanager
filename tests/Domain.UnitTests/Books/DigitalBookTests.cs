using Domain.Books;

namespace Domain.UnitTests.Books;

public class DigitalBookTests
{
    private static readonly Guid DefaultLibraryId = Guid.CreateVersion7();
    private const string DefaultSerie = "Blacksad";
    private const string DefaultTitle = "Quelque part entre les ombres";
    private const string DefaultIsbn = "9782205050196";
    private const string DefaultFilePath = "blacksad/tome-01.cbz";
    private const long DefaultFileSize = 52_428_800L; // 50 MB

    [Fact]
    public void Create_Should_ReturnSuccess_WhenAllParametersAreValid()
    {
        // Act
        var result = DigitalBook.Create(DefaultSerie, DefaultTitle, DefaultIsbn, DefaultLibraryId, DefaultFilePath, DefaultFileSize);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Serie.Should().Be(DefaultSerie);
        result.Value.Title.Should().Be(DefaultTitle);
        result.Value.ISBN.Should().Be(DefaultIsbn);
        result.Value.LibraryId.Should().Be(DefaultLibraryId);
        result.Value.FilePath.Should().Be(DefaultFilePath);
        result.Value.FileSize.Should().Be(DefaultFileSize);
        result.Value.VolumeNumber.Should().Be(1);
        result.Value.ImageLink.Should().BeEmpty();
        result.Value.Authors.Should().BeEmpty();
        result.Value.Publishers.Should().BeEmpty();
        result.Value.PublishDate.Should().BeNull();
        result.Value.NumberOfPages.Should().BeNull();
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenSerieIsEmpty()
    {
        // Act
        var result = DigitalBook.Create(string.Empty, DefaultTitle, DefaultIsbn, DefaultLibraryId, DefaultFilePath, DefaultFileSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenSerieIsWhitespace()
    {
        // Act
        var result = DigitalBook.Create("   ", DefaultTitle, DefaultIsbn, DefaultLibraryId, DefaultFilePath, DefaultFileSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenTitleIsEmpty()
    {
        // Act
        var result = DigitalBook.Create(DefaultSerie, string.Empty, DefaultIsbn, DefaultLibraryId, DefaultFilePath, DefaultFileSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenIsbnIsEmpty()
    {
        // Act
        var result = DigitalBook.Create(DefaultSerie, DefaultTitle, string.Empty, DefaultLibraryId, DefaultFilePath, DefaultFileSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenFilePathIsEmpty()
    {
        // Act
        var result = DigitalBook.Create(DefaultSerie, DefaultTitle, DefaultIsbn, DefaultLibraryId, string.Empty, DefaultFileSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenFilePathIsWhitespace()
    {
        // Act
        var result = DigitalBook.Create(DefaultSerie, DefaultTitle, DefaultIsbn, DefaultLibraryId, "   ", DefaultFileSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenLibraryIdIsEmpty()
    {
        // Act
        var result = DigitalBook.Create(DefaultSerie, DefaultTitle, DefaultIsbn, Guid.Empty, DefaultFilePath, DefaultFileSize);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenFileSizeIsZero()
    {
        // Act
        var result = DigitalBook.Create(DefaultSerie, DefaultTitle, DefaultIsbn, DefaultLibraryId, DefaultFilePath, 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenFileSizeIsNegative()
    {
        // Act
        var result = DigitalBook.Create(DefaultSerie, DefaultTitle, DefaultIsbn, DefaultLibraryId, DefaultFilePath, -1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public void Create_Should_SetDefaultVolumeNumber_WhenNotProvided()
    {
        // Act
        var result = DigitalBook.Create(DefaultSerie, DefaultTitle, DefaultIsbn, DefaultLibraryId, DefaultFilePath, DefaultFileSize);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.VolumeNumber.Should().Be(1);
    }

    [Fact]
    public void Create_Should_SetAllOptionalFields_WhenProvided()
    {
        // Arrange
        const int volumeNumber = 3;
        const string imageLink = "https://cloudinary.com/cover.webp";
        const string authors = "Juan Díaz Canales, Juanjo Guarnido";
        const string publishers = "Dargaud";
        var publishDate = new DateOnly(2000, 11, 1);
        const int numberOfPages = 48;

        // Act
        var result = DigitalBook.Create(
            DefaultSerie, DefaultTitle, DefaultIsbn, DefaultLibraryId, DefaultFilePath, DefaultFileSize,
            volumeNumber, imageLink, authors, publishers, publishDate, numberOfPages);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.VolumeNumber.Should().Be(volumeNumber);
        result.Value.ImageLink.Should().Be(imageLink);
        result.Value.Authors.Should().Be(authors);
        result.Value.Publishers.Should().Be(publishers);
        result.Value.PublishDate.Should().Be(publishDate);
        result.Value.NumberOfPages.Should().Be(numberOfPages);
    }

    [Fact]
    public void Create_Should_GenerateNewId_EachCall()
    {
        // Act
        var result1 = DigitalBook.Create(DefaultSerie, DefaultTitle, DefaultIsbn, DefaultLibraryId, DefaultFilePath, DefaultFileSize);
        var result2 = DigitalBook.Create(DefaultSerie, DefaultTitle, DefaultIsbn, DefaultLibraryId, DefaultFilePath, DefaultFileSize);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value!.Id.Should().NotBe(result2.Value!.Id);
    }
}
