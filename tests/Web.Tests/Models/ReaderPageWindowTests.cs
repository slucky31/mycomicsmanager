using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class ReaderPageWindowTests
{
    private static int[] RequestedPages(ReaderPageWindow window) =>
        Enumerable.Range(0, window.PageCount).Where(window.IsRequested).ToArray();

    [Fact]
    public void RequestAround_Should_RequestThePageAndItsNeighbours()
    {
        var window = new ReaderPageWindow(20);

        window.RequestAround(7);

        RequestedPages(window).Should().Equal(6, 7, 8, 9, 10);
    }

    [Theory]
    [InlineData(0, new[] { 0, 1, 2, 3 })]
    [InlineData(4, new[] { 3, 4 })]
    public void RequestAround_Should_StayWithinTheBook(int pageIndex, int[] expected)
    {
        var window = new ReaderPageWindow(5);

        window.RequestAround(pageIndex);

        RequestedPages(window).Should().Equal(expected);
    }

    [Fact]
    public void RequestAround_Should_KeepThePagesAlreadyRequested()
    {
        var window = new ReaderPageWindow(20);

        window.RequestAround(2);
        window.RequestAround(12);

        RequestedPages(window).Should().Equal(1, 2, 3, 4, 5, 11, 12, 13, 14, 15);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(20)]
    public void IsRequested_Should_ReturnFalse_WhenThePageIsOutsideTheBook(int pageIndex)
    {
        var window = new ReaderPageWindow(20);
        window.RequestAround(0);
        window.RequestAround(19);

        window.IsRequested(pageIndex).Should().BeFalse();
    }

    [Fact]
    public void Constructor_Should_Throw_WhenThePageCountIsNegative()
    {
        var act = () => new ReaderPageWindow(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
