using Base.Integration.Tests;
using Xunit;

namespace Persistence.Tests;

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<IntegrationTestWebAppFactory>
{
}
