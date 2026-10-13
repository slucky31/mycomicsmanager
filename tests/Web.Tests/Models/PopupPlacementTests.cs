using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class PopupPlacementTests
{
    [Fact]
    public void Style_Should_BeEmpty_WhenThereIsNoAnchor() =>
        PopupPlacement.Style(null, alignEnd: true).Should().BeEmpty();

    [Fact]
    public void Style_Should_OpenBelowTheButton_AlignedOnItsRightEdge()
    {
        var button = new PopupAnchor(700, 14, 744, 58, 1024, 768);

        PopupPlacement.Style(button, alignEnd: true)
            .Should().Be("--popup-left: 444px; --popup-top: 62px; --popup-max-height: 698px;");
    }

    [Fact]
    public void Style_Should_OpenAboveTheButton_WhenThereIsMoreRoomAbove()
    {
        var button = new PopupAnchor(900, 600, 944, 644, 1024, 768);

        PopupPlacement.Style(button, alignEnd: true)
            .Should().Be("--popup-left: 644px; --popup-bottom: 172px; --popup-max-height: 588px;");
    }

    [Fact]
    public void Style_Should_StartAtTheFinger_WhenTheBookIsLongPressed()
    {
        var finger = PopupAnchor.At(120.4, 200, 1024, 768);

        PopupPlacement.Style(finger, alignEnd: false)
            .Should().Be("--popup-left: 120px; --popup-top: 204px; --popup-max-height: 556px;");
    }

    [Theory]
    [InlineData(1000, 716)]
    [InlineData(2, 8)]
    public void Style_Should_KeepTheMenuInsideTheViewport(double x, int expectedLeft)
    {
        var finger = PopupAnchor.At(x, 100, 1024, 768);

        PopupPlacement.Style(finger, alignEnd: false).Should().StartWith($"--popup-left: {expectedLeft}px;");
    }
}
