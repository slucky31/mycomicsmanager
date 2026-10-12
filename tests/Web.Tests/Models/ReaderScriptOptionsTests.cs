using AwesomeAssertions;
using Domain.Books;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class ReaderScriptOptionsTests
{
    [Fact]
    public void From_Should_DescribeTheDefaultSettings()
    {
        ReaderScriptOptions.From(ReaderSettings.Default)
            .Should().Be(new ReaderScriptOptions(Paged: false, RightToLeft: false, FitWidth: true, Key: "FitWidth-Continuous-LeftToRight"));
    }

    [Fact]
    public void From_Should_DescribeAMangaReadPageByPage()
    {
        var settings = new ReaderSettings(ReaderLayout.TwoPages, ReaderScrolling.PageByPage, ReadingDirection.RightToLeft);

        ReaderScriptOptions.From(settings)
            .Should().Be(new ReaderScriptOptions(Paged: true, RightToLeft: true, FitWidth: false, Key: "TwoPages-PageByPage-RightToLeft"));
    }

    [Fact]
    public void From_Should_Throw_WhenSettingsAreNull()
    {
        var act = () => ReaderScriptOptions.From(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
