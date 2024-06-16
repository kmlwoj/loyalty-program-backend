using FluentAssertions;
using lojalBackend.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using lojalBackend.DbContexts.MainContext;
using lojalBackend.Tests.Fixtures;
using Xunit;

namespace lojalBackend.Tests.Controllers;

[Collection("WebApplicationFactoryCollection")]
public class ContactControllerAnonymousAuthTests
{
    private readonly HttpClient HttpClient;

    public ContactControllerAnonymousAuthTests(WebApplicationFactoryFixture fixture)
    {
        HttpClient = fixture.CreateClientWithAnonymousAuth();
    }

    [Fact]
    public async Task GetContacts_Anonymously_Returns200Ok()
    {
        // Act
        var response = await HttpClient.GetAsync("/api/Contact/GetContacts");
        // var response2 = await _client.GetFromJsonAsync<ContactInfo[]>("/api/Contact/GetContacts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddContact_WithoutAuthorization_Returns401Unauthorized()
    {
        // Arrange
        var newContact = new ContactInfoModel
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Phone = "123-456-7890",
            Position = "Manager"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/Contact/AddContact", newContact);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteContact_WithoutAuthorization_Returns401Unauthorized()
    {
        // Act
        var response = await HttpClient.DeleteAsync("/api/Contact/DeleteContact?id=999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}