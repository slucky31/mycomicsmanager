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
}
