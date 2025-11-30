using Base.Integration.Tests;
using Xunit;

namespace Persistence.Tests;

[CollectionDefinition("Library")]
public class LibraryCollectionTests : ICollectionFixture<IntegrationTestWebAppFactory>
{
}

[CollectionDefinition("User")]
public class UserCollectionTests : ICollectionFixture<IntegrationTestWebAppFactory>
{
}

[CollectionDefinition("LocalStorage")]
public class LocalStorageCollectionTests : ICollectionFixture<IntegrationTestWebAppFactory>
{
}
