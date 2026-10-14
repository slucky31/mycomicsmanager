using AwesomeAssertions;
using FluentValidation.Results;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class FormErrorsTests
{
    [Fact]
    public void From_Should_KeepTheFirstErrorOfEachField()
    {
        var errors = FormErrors.From(new ValidationResult(
        [
            new ValidationFailure("Title", "Enter a title"),
            new ValidationFailure("ISBN", "Enter the ISBN"),
            new ValidationFailure("ISBN", "Enter a valid ISBN: 10 or 13 digits"),
        ]));

        errors.Any.Should().BeTrue();
        errors.Count.Should().Be(2);
        errors.Summary.Should().Be("2 fields need your attention.");
        errors.For("ISBN").Should().Be("Enter the ISBN");
        errors.AriaInvalid("ISBN").Should().Be("true");
    }

    [Fact]
    public void Summary_Should_SpeakOfOneField_WhenOneFieldIsInvalid()
    {
        var errors = FormErrors.From(new ValidationResult([new ValidationFailure("Serie", "Enter a series")]));

        errors.Summary.Should().Be("1 field needs your attention.");
    }

    [Fact]
    public void None_Should_HaveNoError()
    {
        FormErrors.None.Any.Should().BeFalse();
        FormErrors.None.For("Title").Should().BeNull();
        FormErrors.None.AriaInvalid("Title").Should().BeNull();
    }
}
