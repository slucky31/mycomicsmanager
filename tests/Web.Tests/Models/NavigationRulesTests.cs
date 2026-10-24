using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class NavigationRulesTests
{
    private static readonly Guid s_libraryId = Guid.Parse("01a10db7-5a64-73bc-91be-9aed30dcff5f");

    [Theory]
    [InlineData("statistics?year=2026", "statistics")]
    [InlineData("feed-imports#top", "feed-imports")]
    [InlineData("settings", "settings")]
    public void StripQueryAndFragment_Should_KeepOnlyThePath(string relativePath, string expected)
    {
        NavigationRules.StripQueryAndFragment(relativePath).Should().Be(expected);
    }

    [Theory]
    [InlineData("statistics", "/statistics", true)]
    [InlineData("Statistics?year=2026", "/statistics", true)]
    [InlineData("libraries/list", "/libraries", true)]
    [InlineData("librariesextra", "/libraries", false)]
    [InlineData("import", "/feed-imports", false)]
    [InlineData("", "/", true)]
    [InlineData("settings", "/", false)]
    public void IsActive_Should_MatchThePathAndItsChildren(string relativePath, string path, bool expected)
    {
        NavigationRules.IsActive(relativePath, path).Should().Be(expected);
    }

    [Theory]
    [InlineData("books/add", true)]
    [InlineData("books/add/form?libraryId=1", true)]
    [InlineData("books/add/manual", true)]
    [InlineData("books/01a10db7-5a64-73bc-91be-9aed30dcff5f", false)]
    [InlineData("libraries/list", false)]
    [InlineData("books/01a10db7-5a64-73bc-91be-9aed30dcff5f/find-details?isbn=9782800112343", true)]
    [InlineData("books/01a10db7-5a64-73bc-91be-9aed30dcff5f/find-details/", true)]
    public void HidesBottomBar_Should_HideItOnlyWhileAddingABookOrImportingItsInfo(string relativePath, bool expected)
    {
        NavigationRules.HidesBottomBar(relativePath).Should().Be(expected);
    }

    [Theory]
    [InlineData("libraries/01a10db7-5a64-73bc-91be-9aed30dcff5f")]
    [InlineData("libraries/01a10db7-5a64-73bc-91be-9aed30dcff5f/edit")]
    [InlineData("Libraries/01a10db7-5a64-73bc-91be-9aed30dcff5f?view=list")]
    public void CurrentLibraryId_Should_ReturnTheLibraryOfTheLibraryPages(string relativePath)
    {
        NavigationRules.CurrentLibraryId(relativePath).Should().Be(s_libraryId);
    }

    [Theory]
    [InlineData("libraries/list")]
    [InlineData("libraries/add")]
    [InlineData("books/01a10db7-5a64-73bc-91be-9aed30dcff5f")]
    [InlineData("")]
    public void CurrentLibraryId_Should_ReturnNull_WhenThePageIsNotALibrary(string relativePath)
    {
        NavigationRules.CurrentLibraryId(relativePath).Should().BeNull();
    }
}
