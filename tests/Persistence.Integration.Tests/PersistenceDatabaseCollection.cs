using Base.Integration.Tests;
using Xunit;

namespace Persistence.Tests;

[CollectionDefinition("DatabaseCollectionTests")]
public class DatabaseCollectionTests : ICollectionFixture<IntegrationTestWebAppFactory>
{
}
