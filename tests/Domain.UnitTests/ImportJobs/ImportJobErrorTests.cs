using Domain.ImportJobs;

namespace Domain.UnitTests.ImportJobs;

public class ImportJobErrorTests
{
    [Fact]
    public void IsbnAlreadyOwned_Should_NameTheLibrary_WhenItIsKnown()
    {
        var error = ImportJobError.IsbnAlreadyOwned("9782075162869", "BD numériques");

        error.Code.Should().Be("IMP409D");
        error.Description.Should().Be("ISBN 9782075162869 is already used by a book of the digital library BD numériques.");
    }

    [Fact]
    public void SplitPartiallyDeposited_Should_SayHowManyArchivesWereMoved()
    {
        var error = ImportJobError.SplitPartiallyDeposited(2, 4);

        error.Code.Should().Be("IMP507B");
        error.Description.Should().Be("Only 2 of the 4 archives found inside could be moved to the import folder.");
    }
}
