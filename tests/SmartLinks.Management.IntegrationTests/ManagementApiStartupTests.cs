using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SmartLinks.Management.IntegrationTests;

public sealed class ManagementApiStartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Сохраняет фабрику тестового хоста Management API
    /// </summary>
    public ManagementApiStartupTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Проверяет отсутствие корневого маршрута Management API
    /// </summary>
    [Fact]
    public async Task RootEndpointReturnsNotFound()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}