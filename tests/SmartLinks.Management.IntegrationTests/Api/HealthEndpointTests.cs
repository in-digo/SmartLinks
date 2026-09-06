using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartLinks.Management.IntegrationTests.Infrastructure;

namespace SmartLinks.Management.IntegrationTests.Api;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IClassFixture<PostgreSqlFixture>
{
    private const string _unavailableConnectionString = "Host=127.0.0.1;Port=1;Database=smartlinks;Username=unused;Timeout=1";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly PostgreSqlFixture _postgreSqlFixture;

    /// <summary>
    /// Сохраняет фабрику тестового хоста Management API
    /// </summary>
    public HealthEndpointTests(WebApplicationFactory<Program> factory, PostgreSqlFixture postgreSqlFixture)
    {
        _factory = factory;
        _postgreSqlFixture = postgreSqlFixture;
    }

    /// <summary>
    /// Проверяет доступность liveness endpoint без обращения к внешним зависимостям
    /// </summary>
    [Fact]
    public async Task LivenessEndpointReturnsOk()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Проверяет неготовность Management при недоступности PostgreSQL
    /// </summary>
    [Fact]
    public async Task ReadinessEndpointReturnsServiceUnavailableWhenPostgreSqlIsUnavailable()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Management", _unavailableConnectionString));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    /// <summary>
    /// Проверяет готовность Management при доступности PostgreSQL
    /// </summary>
    [Fact]
    public async Task ReadinessEndpointReturnsOkWhenPostgreSqlIsAvailable()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Management", _postgreSqlFixture.ConnectionString));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}