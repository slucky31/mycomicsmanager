using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class DisplayDiagnosticsViewTests
{
    [Theory]
    [InlineData(true, "Home Screen app")]
    [InlineData(false, "Browser")]
    public void Summary_Should_SayWhereThePageRuns(bool appMode, string expected)
    {
        var diagnostics = new DisplayDiagnostics(false, "browser", false, appMode, 0, 0, 700, 852);

        DisplayDiagnosticsView.Summary(diagnostics).Should().Be(expected);
    }

    [Fact]
    public void Rows_Should_ListEverySignal_WithRoundedPixels()
    {
        var diagnostics = new DisplayDiagnostics(false, "standalone", true, true, 58.6, 87.2, 852, 852);

        DisplayDiagnosticsView.Rows(diagnostics).Should().Equal(
            new DisplayDiagnosticRow("Home Screen app", "Yes"),
            new DisplayDiagnosticRow("navigator.standalone", "No"),
            new DisplayDiagnosticRow("Display mode", "standalone"),
            new DisplayDiagnosticRow("Window fills the screen", "Yes (852 / 852 px)"),
            new DisplayDiagnosticRow("Status bar", "59 px"),
            new DisplayDiagnosticRow("Space above the bars", "87 px"));
    }

    [Fact]
    public void Summary_Should_Throw_WhenTheDiagnosticsAreMissing()
    {
        var summary = () => DisplayDiagnosticsView.Summary(null!);
        var rows = () => DisplayDiagnosticsView.Rows(null!);

        summary.Should().Throw<ArgumentNullException>();
        rows.Should().Throw<ArgumentNullException>();
    }
}
