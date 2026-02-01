using AwesomeAssertions;
using Web.Validators;
using Xunit;

namespace Web.Tests.Validators;

public sealed class BookValidatorTests
{
    private readonly BookValidator _validator;

    public BookValidatorTests()
    {
        _validator = new BookValidator();
    }

    #region Title Validation Tests

    [Fact]
    public void Validate_ShouldReturnError_WhenTitleIsEmpty()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = string.Empty,
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(BookUiDto.Title));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Title is required.");
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenTitleExceedsMaxLength()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = new string('A', 201),
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(BookUiDto.Title));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Title must not exceed 200 characters.");
    }

    [Fact]
    public void Validate_ShouldPass_WhenTitleIsAtMaxLength()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = new string('A', 200),
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenTitleIsValid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Valid Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region ISBN Validation Tests

    [Fact]
    public void Validate_ShouldReturnError_WhenISBNIsEmpty()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = string.Empty,
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BookUiDto.ISBN) && e.ErrorMessage == "ISBN is required.");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BookUiDto.ISBN) && e.ErrorMessage == "ISBN must be a valid 10 or 13 digit number.");
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenISBNExceedsMaxLength()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = new string('1', 21),
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(BookUiDto.ISBN) && e.ErrorMessage == "ISBN must not exceed 20 characters.");
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenISBNIsInvalid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "invalid",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(BookUiDto.ISBN) && e.ErrorMessage == "ISBN must be a valid 10 or 13 digit number.");
    }

    [Fact]
    public void Validate_ShouldPass_WhenISBN10IsValid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenISBN13IsValid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "9780306406157",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenISBN10WithXIsValid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "043942089X",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenISBNWithDashesIsValid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "978-0-306-40615-7",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Serie Validation Tests

    [Fact]
    public void Validate_ShouldReturnError_WhenSerieIsEmpty()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = string.Empty,
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(BookUiDto.Serie));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Serie is required.");
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenSerieExceedsMaxLength()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = new string('A', 201),
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(BookUiDto.Serie));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Serie must not exceed 200 characters.");
    }

    [Fact]
    public void Validate_ShouldPass_WhenSerieIsAtMaxLength()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = new string('A', 200),
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenSerieIsValid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Valid Serie",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region VolumeNumber Validation Tests

    [Fact]
    public void Validate_ShouldReturnError_WhenVolumeNumberIsZero()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 0
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(BookUiDto.VolumeNumber));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Volume number must be greater than 0.");
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenVolumeNumberIsNegative()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = -1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(BookUiDto.VolumeNumber));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Volume number must be greater than 0.");
    }

    [Fact]
    public void Validate_ShouldPass_WhenVolumeNumberIsOne()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenVolumeNumberIsLarge()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 999
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region ImageLink Validation Tests

    [Fact]
    public void Validate_ShouldReturnError_WhenImageLinkExceedsMaxLength()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1,
            ImageLink = new string('A', 501)
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(BookUiDto.ImageLink));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Image link must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_ShouldPass_WhenImageLinkIsEmpty()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1,
            ImageLink = string.Empty
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenImageLinkIsAtMaxLength()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1,
            ImageLink = new string('A', 500)
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenImageLinkIsValid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1,
            ImageLink = "https://example.com/image.jpg"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region ValidateValue Method Tests

    [Fact]
    public async Task ValidateValue_ShouldReturnError_WhenModelIsNotBookUiDto()
    {
        // Arrange
        var invalidModel = new object();
        const string propertyName = "Title";

        // Act
        var result = await _validator.ValidateValue(invalidModel, propertyName);

        // Assert
        result.Should().ContainSingle();
        result.Should().Contain("Invalid model for validation.");
    }

    [Fact]
    public async Task ValidateValue_ShouldReturnError_WhenPropertyNameIsNull()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = await _validator.ValidateValue(dto, null!);

        // Assert
        result.Should().ContainSingle();
        result.Should().Contain("Property name is required.");
    }

    [Fact]
    public async Task ValidateValue_ShouldReturnError_WhenPropertyNameIsEmpty()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = await _validator.ValidateValue(dto, string.Empty);

        // Assert
        result.Should().ContainSingle();
        result.Should().Contain("Property name is required.");
    }

    [Fact]
    public async Task ValidateValue_ShouldReturnError_WhenPropertyNameIsWhitespace()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = await _validator.ValidateValue(dto, "   ");

        // Assert
        result.Should().ContainSingle();
        result.Should().Contain("Property name is required.");
    }

    [Fact]
    public async Task ValidateValue_ShouldReturnEmpty_WhenPropertyIsValid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = await _validator.ValidateValue(dto, nameof(BookUiDto.Title));

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateValue_ShouldReturnErrors_WhenTitleIsInvalid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = string.Empty,
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = await _validator.ValidateValue(dto, nameof(BookUiDto.Title));

        // Assert
        result.Should().ContainSingle();
        result.Should().Contain("Title is required.");
    }

    [Fact]
    public async Task ValidateValue_ShouldReturnErrors_WhenISBNIsInvalid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "invalid",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = await _validator.ValidateValue(dto, nameof(BookUiDto.ISBN));

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("ISBN must be a valid 10 or 13 digit number.");
    }

    [Fact]
    public async Task ValidateValue_ShouldReturnErrors_WhenSerieIsInvalid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = string.Empty,
            VolumeNumber = 1
        };

        // Act
        var result = await _validator.ValidateValue(dto, nameof(BookUiDto.Serie));

        // Assert
        result.Should().ContainSingle();
        result.Should().Contain("Serie is required.");
    }

    [Fact]
    public async Task ValidateValue_ShouldReturnErrors_WhenVolumeNumberIsInvalid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 0
        };

        // Act
        var result = await _validator.ValidateValue(dto, nameof(BookUiDto.VolumeNumber));

        // Assert
        result.Should().ContainSingle();
        result.Should().Contain("Volume number must be greater than 0.");
    }

    [Fact]
    public async Task ValidateValue_ShouldReturnErrors_WhenImageLinkIsInvalid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1,
            ImageLink = new string('A', 501)
        };

        // Act
        var result = await _validator.ValidateValue(dto, nameof(BookUiDto.ImageLink));

        // Assert
        result.Should().ContainSingle();
        result.Should().Contain("Image link must not exceed 500 characters.");
    }

    [Fact]
    public async Task ValidateValue_ShouldReturnEmpty_WhenImageLinkIsValidAndEmpty()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1,
            ImageLink = string.Empty
        };

        // Act
        var result = await _validator.ValidateValue(dto, nameof(BookUiDto.ImageLink));

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public void Validate_ShouldReturnMultipleErrors_WhenMultipleFieldsAreInvalid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = string.Empty,
            ISBN = string.Empty,
            Serie = string.Empty,
            VolumeNumber = 0
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(4);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BookUiDto.Title));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BookUiDto.ISBN));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BookUiDto.Serie));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BookUiDto.VolumeNumber));
    }

    [Fact]
    public void Validate_ShouldReturnAllISBNErrors_WhenISBNIsEmptyAndInvalid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = string.Empty,
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Where(e => e.PropertyName == nameof(BookUiDto.ISBN)).Should().HaveCount(2);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "The Dark Knight Returns",
            ISBN = "978-1401263119",
            Serie = "Batman",
            VolumeNumber = 1,
            ImageLink = "https://example.com/image.jpg"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldHandleSpecialCharacters_InTitle()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Ñoño's Ädventure! & Friends",
            ISBN = "0306406152",
            Serie = "Test Serie",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHandleSpecialCharacters_InSerie()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "Test Title",
            ISBN = "0306406152",
            Serie = "Série écrite en français",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHandleMinimumValidInput()
    {
        // Arrange
        var dto = new BookUiDto
        {
            Title = "A",
            ISBN = "0306406152",
            Serie = "B",
            VolumeNumber = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
