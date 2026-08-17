using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SmartLinks.Management.IntegrationTests;

public sealed class ManagementApiStartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    // Сохраняет фабрику тестового хоста Management API
    public ManagementApiStartupTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // Проверяет запуск Management API и отсутствие пока не настроенного корневого маршрута
    [Fact]
    public async Task RootEndpointReturnsNotFoundWhenRoutesAreNotConfigured()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}