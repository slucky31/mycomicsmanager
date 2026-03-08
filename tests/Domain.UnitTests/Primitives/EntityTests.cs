using Domain.Books;

namespace Domain.UnitTests.Primitives;

public class EntityTests
{
    private static readonly Guid DefaultLibraryId = Guid.CreateVersion7();

    private static PhysicalBook CreateBook() =>
        PhysicalBook.Create("Series", "Title", "9781401245252", libraryId: DefaultLibraryId).Value!;

    [Fact]
    public void CloneAuditable_Should_CopyCreatedOnUtcAndModifiedOnUtc_WhenSourceIsValid()
    {
        // Arrange
        var book = CreateBook();
        var source = CreateBook();
        var createdOn = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var modifiedOn = new DateTime(2024, 6, 20, 14, 30, 0, DateTimeKind.Utc);
        source.CreatedOnUtc = createdOn;
        source.ModifiedOnUtc = modifiedOn;

        // Act
        book.CloneAuditable(source);

        // Assert
        book.CreatedOnUtc.Should().Be(createdOn);
        book.ModifiedOnUtc.Should().Be(modifiedOn);
    }

    [Fact]
    public void CloneAuditable_Should_CopyNullModifiedOnUtc_WhenSourceHasNoModifiedDate()
    {
        // Arrange
        var book = CreateBook();
        var source = CreateBook();
        var createdOn = new DateTime(2024, 3, 1, 8, 0, 0, DateTimeKind.Utc);
        source.CreatedOnUtc = createdOn;
        source.ModifiedOnUtc = null;

        // Act
        book.CloneAuditable(source);

        // Assert
        book.CreatedOnUtc.Should().Be(createdOn);
        book.ModifiedOnUtc.Should().BeNull();
    }

    [Fact]
    public void CloneAuditable_Should_Throw_WhenAuditableIsNull()
    {
        // Arrange
        var book = CreateBook();

        // Act
        var act = () => book.CloneAuditable(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
