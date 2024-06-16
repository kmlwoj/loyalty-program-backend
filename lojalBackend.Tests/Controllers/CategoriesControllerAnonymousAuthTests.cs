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
public class CategoriesControllerAnonymousAuthTests
{
    private readonly HttpClient anonymousHttpClient;

    public CategoriesControllerAnonymousAuthTests(WebApplicationFactoryFixture fixture)
    {
        anonymousHttpClient = fixture.CreateClientWithAnonymousAuth();
    }

    [Fact]
    public async Task GetCategories_WithoutAuthorization_Returns401Unauthorized()
    {
        // Act
        var response = await anonymousHttpClient.GetAsync("/api/Categories/GetCategories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddCategory_WithoutAuthorization_Returns401Unauthorized()
    {
        // Act
        var response = await anonymousHttpClient.PostAsJsonAsync("/api/Categories/AddCategory", "NewCategory");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteCategory_WithoutAuthorization_Returns401Unauthorized()
    {
        // Act
        var response = await anonymousHttpClient.DeleteAsync("/api/Categories/DeleteCategory?category=NewCategory");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCategoryImage_WithoutAuthorization_Returns401Unauthorized()
    {
        // Act
        var response = await anonymousHttpClient.GetAsync("/api/Categories/GetCategoryImage/NewCategory");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetCategoryImage_WithoutAuthorization_Returns401Unauthorized()
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
        var response = await anonymousHttpClient.PutAsync("/api/Categories/SetCategoryImage/NewCategory", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteCategoryImage_WithoutAuthorization_Returns401Unauthorized()
    {
        // Act
        var response = await anonymousHttpClient.DeleteAsync("/api/Categories/DeleteCategoryImage/NewCategory");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
