namespace lojalBackend.Tests.Fixtures;

[CollectionDefinition("WebApplicationFactoryCollection")]
public class WebApplicationFactoryCollectionFixture : ICollectionFixture<WebApplicationFactoryFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
    // https://xunit.net/docs/shared-context#collection-fixture
    // Every test class that uses this collection
    // will share the same instance of HttpClientFixture
    // i.e. the same WebApplicationFactory<Program> factory instance
    // i.e. the same test server instance
}