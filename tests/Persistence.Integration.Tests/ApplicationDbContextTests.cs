using Base.Integration.Tests;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Tests;

[Collection("DatabaseCollectionTests")]
public class ApplicationDbContextTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    // Fails when the model changed without a migration, and kills the mutants of the model configuration.
    [Fact]
    public void HasPendingModelChanges_Should_BeFalse_WhenEveryModelChangeHasItsMigration()
    {
        Context.Database.HasPendingModelChanges().Should().BeFalse();
    }
}
