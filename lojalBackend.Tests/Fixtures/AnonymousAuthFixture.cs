namespace lojalBackend.Tests.Fixtures;

public class AnonymousAuthFixture : IClassFixture<lojalBackendWebApplicationFactory>
{
    private readonly lojalBackendWebApplicationFactory _fixtureFactory;

    public AnonymousAuthFixture(lojalBackendWebApplicationFactory fixtureFactory)
    {
        _fixtureFactory = fixtureFactory;
    }
}