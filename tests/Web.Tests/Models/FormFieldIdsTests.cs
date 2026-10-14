using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class FormFieldIdsTests
{
    [Theory]
    [InlineData("Enter a title", "Separate names with commas", "title-error title-help")]
    [InlineData("Enter a title", null, "title-error")]
    [InlineData(null, "Separate names with commas", "title-help")]
    [InlineData(null, null, null)]
    public void DescribedBy_Should_ListTheTextsUnderTheField(string? error, string? help, string? expected)
    {
        FormFieldIds.DescribedBy("title", error, help).Should().Be(expected);
    }
}
