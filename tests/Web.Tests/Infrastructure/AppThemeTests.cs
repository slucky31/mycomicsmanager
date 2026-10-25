using AwesomeAssertions;
using Web.Infrastructure;
using Xunit;

namespace Web.Tests.Infrastructure;

public sealed class AppThemeTests
{
    [Fact]
    public void Theme_Should_WriteTheButtonsInSentenceCase()
    {
        AppTheme.Theme.Typography.Button.TextTransform.Should().Be("none");
    }
}
