using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SmartLinks.Redirect.IntegrationTests;

public sealed class RedirectApiStartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    // Сохраняет фабрику тестового хоста Redirect API
    public RedirectApiStartupTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // Проверяет запуск Redirect API и отсутствие пока не настроенного корневого маршрута
    [Fact]
    public async Task RootEndpointReturnsNotFoundWhenRoutesAreNotConfigured()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}