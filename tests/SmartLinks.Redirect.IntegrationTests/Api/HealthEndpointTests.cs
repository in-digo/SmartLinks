using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SmartLinks.Redirect.Infrastructure.Synchronization;

namespace SmartLinks.Redirect.IntegrationTests.Api;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Сохраняет фабрику тестового хоста Redirect API
    /// </summary>
    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Проверяет доступность liveness endpoint без внешних зависимостей
    /// </summary>
    [Fact]
    public async Task LivenessEndpointReturnsOk()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Проверяет неготовность Redirect до первоначальной синхронизации
    /// </summary>
    [Fact]
    public async Task ReadinessEndpointReturnsServiceUnavailableBeforeInitialSynchronization()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    /// <summary>
    /// Проверяет готовность Redirect после первоначальной синхронизации
    /// </summary>
    [Fact]
    public async Task ReadinessEndpointReturnsOkAfterInitialSynchronization()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        using var client = factory.CreateClient();
        var synchronizationState = factory.Services.GetRequiredService<ConfigurationSynchronizationState>();

        synchronizationState.MarkReady();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}