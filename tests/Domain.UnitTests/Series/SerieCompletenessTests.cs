using Domain.Series;

namespace Domain.UnitTests.Series;

public class SerieCompletenessTests
{
    [Fact]
    public void Of_Should_BeIncomplete_WhenVolumesAreMissingBetweenTheOwnedOnes()
    {
        var completeness = SerieCompleteness.Of([1, 2, 5, 3, 9], totalVolumes: null);

        completeness.Status.Should().Be(SerieCompletenessStatus.Incomplete);
        completeness.Missing.Should().Equal(new VolumeRange(4, 4), new VolumeRange(6, 8));
        completeness.MissingCount.Should().Be(4);
        completeness.OwnedCount.Should().Be(5);
        completeness.ExpectedCount.Should().Be(9);
    }

    [Fact]
    public void Of_Should_BeUnknown_WhenNothingIsMissingAndTheTotalIsUnknown()
    {
        var completeness = SerieCompleteness.Of([3, 1, 2], totalVolumes: null);

        completeness.Status.Should().Be(SerieCompletenessStatus.Unknown);
        completeness.Missing.Should().BeEmpty();
        completeness.ExpectedCount.Should().Be(3);
    }

    [Fact]
    public void Of_Should_BeComplete_WhenEveryPublishedVolumeIsOwned()
    {
        var completeness = SerieCompleteness.Of([1, 2, 3], totalVolumes: 3);

        completeness.Status.Should().Be(SerieCompletenessStatus.Complete);
        completeness.MissingCount.Should().Be(0);
    }

    [Fact]
    public void Of_Should_MissTheVolumesAfterTheLastOwned_WhenTheTotalIsHigher()
    {
        var completeness = SerieCompleteness.Of([1, 2], totalVolumes: 5);

        completeness.Status.Should().Be(SerieCompletenessStatus.Incomplete);
        completeness.Missing.Should().Equal(new VolumeRange(3, 5));
        completeness.ExpectedCount.Should().Be(5);
    }

    [Fact]
    public void Of_Should_ExpectTheLastOwnedVolume_WhenTheTotalIsLower()
    {
        var completeness = SerieCompleteness.Of([1, 2, 3, 4], totalVolumes: 2);

        completeness.Status.Should().Be(SerieCompletenessStatus.Complete);
        completeness.ExpectedCount.Should().Be(4);
    }

    [Fact]
    public void Of_Should_CountAVolumeOwnedTwiceOnce_AndIgnoreVolumesBelowOne()
    {
        var completeness = SerieCompleteness.Of([0, -1, 2, 2, 1], totalVolumes: null);

        completeness.OwnedCount.Should().Be(2);
        completeness.Status.Should().Be(SerieCompletenessStatus.Unknown);
    }

    [Fact]
    public void Of_Should_MissEveryVolume_WhenNoneIsOwnedAndTheTotalIsKnown()
    {
        var completeness = SerieCompleteness.Of([0], totalVolumes: 2);

        completeness.Missing.Should().Equal(new VolumeRange(1, 2));
        completeness.OwnedCount.Should().Be(0);
    }

    [Fact]
    public void Of_Should_Throw_WhenTheVolumesAreNull()
    {
        var act = () => SerieCompleteness.Of(null!, totalVolumes: null);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NotApplicable_Should_MissNothing()
    {
        SerieCompleteness.NotApplicable.Status.Should().Be(SerieCompletenessStatus.NotApplicable);
        SerieCompleteness.NotApplicable.MissingCount.Should().Be(0);
    }
}
