using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SmartLinks.Management.IntegrationTests.Api;

public sealed class OpenApiEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Сохраняет фабрику тестового хоста Management API
    /// </summary>
    public OpenApiEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Проверяет доступность OpenAPI-документа и его основные метаданные
    /// </summary>
    [Fact]
    public async Task OpenApiDocumentEndpointReturnsDocumentMetadata()
    {
        using var document = await GetOpenApiDocumentAsync();

        var info = document.RootElement.GetProperty("info");

        Assert.Equal("SmartLinks Management API", info.GetProperty("title").GetString());
        Assert.Equal("v1", info.GetProperty("version").GetString());
    }

    /// <summary>
    /// Проверяет доступность Swagger UI
    /// </summary>
    [Fact]
    public async Task SwaggerUiEndpointReturnsOk()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Swagger UI", content);
    }

    /// <summary>
    /// Проверяет наличие публичных и внутренних маршрутов в OpenAPI
    /// </summary>
    [Fact]
    public async Task OpenApiDocumentContainsManagementRoutes()
    {
        using var document = await GetOpenApiDocumentAsync();

        var paths = document.RootElement.GetProperty("paths");

        Assert.True(
            paths.GetProperty("/api/smart-links")
                .TryGetProperty("post", out _));
        Assert.True(
            paths.GetProperty("/api/smart-links/{id}")
                .TryGetProperty("get", out _));
        Assert.True(
            paths.GetProperty("/api/smart-links/{id}")
                .TryGetProperty("put", out _));
        Assert.True(
            paths.GetProperty("/api/smart-links/{id}/publish")
                .TryGetProperty("post", out _));
        Assert.True(
            paths.GetProperty("/internal/configurations/snapshot")
                .TryGetProperty("get", out _));
        Assert.True(
            paths.GetProperty("/internal/configurations/changes")
                .TryGetProperty("get", out _));
    }

    /// <summary>
    /// Проверяет описание API-key только у изменяющих операций
    /// </summary>
    [Fact]
    public async Task OpenApiDocumentDescribesApiKeyForMutatingOperations()
    {
        using var document = await GetOpenApiDocumentAsync();

        var root = document.RootElement;
        var apiKeyScheme = root
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("ApiKey");

        Assert.Equal("apiKey", apiKeyScheme.GetProperty("type").GetString());
        Assert.Equal("header", apiKeyScheme.GetProperty("in").GetString());
        Assert.Equal("X-Api-Key", apiKeyScheme.GetProperty("name").GetString());

        var paths = root.GetProperty("paths");

        AssertApiKeySecurity(
            paths.GetProperty("/api/smart-links")
                .GetProperty("post"));
        AssertApiKeySecurity(
            paths.GetProperty("/api/smart-links/{id}")
                .GetProperty("put"));
        AssertApiKeySecurity(
            paths.GetProperty("/api/smart-links/{id}/publish")
                .GetProperty("post"));

        Assert.False(
            paths.GetProperty("/api/smart-links/{id}")
                .GetProperty("get")
                .TryGetProperty("security", out _));
        Assert.False(
            paths.GetProperty("/internal/configurations/snapshot")
                .GetProperty("get")
                .TryGetProperty("security", out _));
        Assert.False(
            paths.GetProperty("/internal/configurations/changes")
                .GetProperty("get")
                .TryGetProperty("security", out _));
    }

    /// <summary>
    /// Загружает и разбирает OpenAPI-документ Management API
    /// </summary>
    private async Task<JsonDocument> GetOpenApiDocumentAsync()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        return JsonDocument.Parse(content);
    }

    /// <summary>
    /// Проверяет требование схемы API-key у операции OpenAPI
    /// </summary>
    private static void AssertApiKeySecurity(JsonElement operation)
    {
        var security = operation.GetProperty("security");

        var requirement = Assert.Single(security.EnumerateArray());
        var scheme = Assert.Single(requirement.EnumerateObject());

        Assert.Equal("ApiKey", scheme.Name);
    }
}