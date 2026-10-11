using Application.Helpers;

namespace Application.UnitTests.Helpers;

public class IsbnScanPagesTests
{
    [Fact]
    public void GetScanOrder_Should_StartWithTheMostLikelyPages_WhenTheBookIsLong()
    {
        IsbnScanPages.GetScanOrder(48).Should().Equal(1, 2, 47, 46, 0, 3, 4, 45, 44, 43);
    }

    [Fact]
    public void GetScanOrder_Should_ListEachPageOnce_WhenTheBookIsShorterThanBothEnds()
    {
        IsbnScanPages.GetScanOrder(6).Should().Equal(1, 2, 5, 4, 0, 3);
    }

    [Fact]
    public void GetScanOrder_Should_OnlyKeepPagesOfBothEnds_WhenFewPagesAreScanned()
    {
        IsbnScanPages.GetScanOrder(48, pagesFromEachEnd: 2).Should().Equal(1, 47, 46, 0);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-1, 5)]
    [InlineData(10, 0)]
    public void GetScanOrder_Should_ReturnNoPage_WhenThereIsNothingToScan(int pageCount, int pagesFromEachEnd)
    {
        IsbnScanPages.GetScanOrder(pageCount, pagesFromEachEnd).Should().BeEmpty();
    }

    [Fact]
    public void GetScanOrder_Should_ReturnTheOnlyPage_WhenTheBookHasOnePage()
    {
        IsbnScanPages.GetScanOrder(1).Should().Equal(0);
    }

    [Fact]
    public void GetPagesInReadingOrder_Should_SortTheScannedPages()
    {
        IsbnScanPages.GetPagesInReadingOrder(48).Should().Equal(0, 1, 2, 3, 4, 43, 44, 45, 46, 47);
    }
}
