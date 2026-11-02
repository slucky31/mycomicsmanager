using Application.Helpers;

namespace Application.UnitTests.Helpers;

public class FileNameTitleHelperTests
{
    [Theory]
    [InlineData("Ashita.no.Joe.T08.cbz", "Ashita no Joe T08")]
    [InlineData("Blacksad [BD.fr] T03.cbr", "Blacksad T03")]
    [InlineData("Thorgal (2024) (Re-Up).pdf", "Thorgal")]
    [InlineData("Les.Vieux.Fourneaux.[FR].(Dargaud).T08.cbz", "Les Vieux Fourneaux T08")]
    [InlineData("  Maus  .cbz", "Maus")]
    public void ToTitle_Should_ReplaceDotsAndDropBracketGroups(string fileName, string expected)
    {
        FileNameTitleHelper.ToTitle(fileName).Should().Be(expected);
    }

    [Fact]
    public void ToTitle_Should_KeepTheName_WhenOnlyBracketGroupsRemain()
    {
        FileNameTitleHelper.ToTitle("[Scan].cbz").Should().Be("[Scan]");
    }
}
