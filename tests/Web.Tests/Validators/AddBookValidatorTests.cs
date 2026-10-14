using AwesomeAssertions;
using Web.Validators;
using Xunit;

namespace Web.Tests.Validators;

public sealed class AddBookValidatorTests
{
    private readonly AddBookValidator _validator = new();

    private static BookUiDto Book(Guid libraryId) => new()
    {
        Title = "Extremity",
        Serie = "Extremity",
        ISBN = "9782413015406",
        VolumeNumber = 1,
        LibraryId = libraryId,
    };

    [Fact]
    public void Validate_Should_AskForALibrary_WhenNoneIsChosen()
    {
        var result = _validator.Validate(Book(Guid.Empty));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(BookUiDto.LibraryId) && e.ErrorMessage == "Choose a library");
    }

    [Fact]
    public void Validate_Should_Pass_WhenALibraryIsChosen()
    {
        _validator.Validate(Book(Guid.CreateVersion7())).IsValid.Should().BeTrue();
    }
}
