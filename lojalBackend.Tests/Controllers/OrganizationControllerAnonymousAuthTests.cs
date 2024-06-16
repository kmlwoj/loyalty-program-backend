using FluentAssertions;
using lojalBackend.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using lojalBackend.DbContexts.MainContext;
using lojalBackend.Tests.Fixtures;
using Xunit;

namespace lojalBackend.Tests.Controllers;

[Collection("WebApplicationFactoryCollection")]
public class OrganizationControllerAnonymousAuthTests
{
    private readonly HttpClient anonymousHttpClient;

    public OrganizationControllerAnonymousAuthTests(WebApplicationFactoryFixture fixture)
    {
        anonymousHttpClient = fixture.CreateClientWithAnonymousAuth();
    }

    [Fact]
    public async Task GetOrganizations_WithoutAuthorization_Returns401Unauthorized()
    {
        // Act
        var response = await anonymousHttpClient.GetAsync("/api/Organization/GetOrganizations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteOrganization_WithoutAuthorization_Returns401Unauthorized()
    {
        // Act
        var response = await anonymousHttpClient.DeleteAsync("/api/Organization/DeleteOrganization?id=999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrganizations_WithInvalidEndpoint_Returns404NotFound()
    {
        // Act
        var response = await anonymousHttpClient.GetAsync("/api/Organization/InvalidEndpoint");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddOrganization_WithoutAuthorization_Returns401Unauthorized()
    {
        // Arrange
        var newOrganization = new OrganizationModel
        {
            Name = "NewOrg",
            Type = OrgTypes.Shop
        };

        // Act
        var response = await anonymousHttpClient.PostAsJsonAsync("/api/Organization/AddOrganization", newOrganization);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrganizationImage_WithoutAuthorization_Returns401Unauthorized()
    {
        // Act
        var response = await anonymousHttpClient.GetAsync("/api/Organization/GetOrganizationImage/NewOrg");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetOrganizationImage_WithoutAuthorization_Returns401Unauthorized()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[0]); // Empty file content for the sake of testing
        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "file",
            FileName = "test.jpg"
        };
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent);

        // Act
        var response = await anonymousHttpClient.PutAsync("/api/Organization/SetOrganizationImage/NewOrg", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteOrganizationImage_WithoutAuthorization_Returns401Unauthorized()
    {
        // Act
        var response = await anonymousHttpClient.DeleteAsync("/api/Organization/DeleteOrganizationImage/NewOrg");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
