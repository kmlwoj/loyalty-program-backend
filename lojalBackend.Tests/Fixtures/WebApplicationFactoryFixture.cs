using System.Net.Http.Headers;

namespace lojalBackend.Tests.Fixtures;

public class WebApplicationFactoryFixture
{
    /// <summary>
    ///  The factory with test server shared among tests.
    /// </summary>
    private lojalBackendWebApplicationFactory factory;

    public WebApplicationFactoryFixture()
    {
        factory = new lojalBackendWebApplicationFactory();
    }

    public HttpClient CreateClientWithAnonymousAuth()
    {
        return factory.CreateClient();
    }
}