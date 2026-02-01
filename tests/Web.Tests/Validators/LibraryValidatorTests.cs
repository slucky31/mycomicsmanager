using AwesomeAssertions;
using Web.Validators;
using Xunit;

namespace Web.Tests.Validators;

public sealed class LibraryValidatorTests
{
    private readonly LibraryValidator _validator;

    public LibraryValidatorTests()
    {
        _validator = new LibraryValidator();
    }

    #region Name Validation Tests

    [Fact]
    public void Validate_ShouldReturnError_WhenNameIsEmpty()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = string.Empty
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(LibraryUiDto.Name));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Name is required.");
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenNameExceedsMaxLength()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = new string('A', 101)
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(LibraryUiDto.Name));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Name must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_ShouldPass_WhenNameIsAtMaxLength()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = new string('A', 100)
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenNameIsValid()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = "Marvel Comics"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region RelativePath Validation Tests

    [Fact]
    public void Validate_ShouldReturnError_WhenRelativePathExceedsMaxLength()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = new string('A', 101)
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LibraryUiDto.RelativePath));
        result.Errors.Should().Contain(e => e.ErrorMessage == "RelativePath must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_ShouldPass_WhenRelativePathIsAtMaxLength()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = new string('A', 100)
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenRelativePathIsValid()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = "DC Comics"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region ValidateValue Method Tests

    [Fact]
    public async Task ValidateValue_ShouldReturnError_WhenModelIsNotLibraryUiDto()
    {
        // Arrange
        var invalidModel = new object();
        const string propertyName = "Name";

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
        var dto = new LibraryUiDto
        {
            Name = "Test Library"
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
        var dto = new LibraryUiDto
        {
            Name = "Test Library"
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
        var dto = new LibraryUiDto
        {
            Name = "Test Library"
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
        var dto = new LibraryUiDto
        {
            Name = "Test Library"
        };

        // Act
        var result = await _validator.ValidateValue(dto, nameof(LibraryUiDto.Name));

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateValue_ShouldReturnErrors_WhenNameIsInvalid()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = string.Empty
        };

        // Act
        var result = await _validator.ValidateValue(dto, nameof(LibraryUiDto.Name));

        // Assert
        result.Should().ContainSingle();
        result.Should().Contain("Name is required.");
    }

    [Fact]
    public async Task ValidateValue_ShouldReturnErrors_WhenRelativePathIsInvalid()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = new string('A', 101)
        };

        // Act
        var result = await _validator.ValidateValue(dto, nameof(LibraryUiDto.RelativePath));

        // Assert
        result.Should().ContainSingle();
        result.Should().Contain("RelativePath must not exceed 100 characters.");
    }

    [Fact]
    public async Task ValidateValue_ShouldReturnEmpty_WhenRelativePathIsValid()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = "Valid Library"
        };

        // Act
        var result = await _validator.ValidateValue(dto, nameof(LibraryUiDto.RelativePath));

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public void Validate_ShouldReturnMultipleErrors_WhenNameIsEmpty()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = string.Empty
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(LibraryUiDto.Name));
    }

    [Fact]
    public void Validate_ShouldReturnMultipleErrors_WhenNameExceedsMaxLength()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = new string('A', 101)
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LibraryUiDto.Name));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LibraryUiDto.RelativePath));
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = "Dark Horse Comics"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldHandleSpecialCharacters_InName()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = "Bibliothèque française & européenne"
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
        var dto = new LibraryUiDto
        {
            Name = "A"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHandleNumericCharacters()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = "Library 2024"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHandleMixedCase()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = "MiXeD CaSe LiBrArY"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHandleWhitespace()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = "  Library With Spaces  "
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHandleAccentedCharacters()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = "éèêëÈÉÊË-ûüùÛÜÙ"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateValue_ShouldValidateSingleProperty()
    {
        // Arrange
        var dto = new LibraryUiDto
        {
            Name = new string('A', 101)
        };

        // Act
        var nameResult = await _validator.ValidateValue(dto, nameof(LibraryUiDto.Name));
        var relativePathResult = await _validator.ValidateValue(dto, nameof(LibraryUiDto.RelativePath));

        // Assert
        nameResult.Should().Contain("Name must not exceed 100 characters.");
        relativePathResult.Should().Contain("RelativePath must not exceed 100 characters.");
    }

    #endregion
}
