using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartLinks.Management.Infrastructure.Persistence;
using SmartLinks.Management.IntegrationTests.Infrastructure;

namespace SmartLinks.Management.IntegrationTests.Api;

public sealed class SmartLinkEndpointsTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime, IDisposable
{
    private const string _apiKey = "integration-test-api-key";
    private readonly PostgreSqlFixture _fixture;
    private bool _disposed;
    private ManagementApiFactory _factory = null!;

    /// <summary>
    /// Инициализирует интеграционные тесты HTTP endpoints умных ссылок
    /// </summary>
    public SmartLinkEndpointsTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Создаёт тестовый хост и применяет миграции Management
    /// </summary>
    public async Task InitializeAsync()
    {
        _factory = new ManagementApiFactory(_fixture.ConnectionString, _apiKey);

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _factory.Dispose();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Проверяет запрет создания умной ссылки без API-ключа
    /// </summary>
    [Fact]
    public async Task CreateEndpointReturnsUnauthorizedWhenApiKeyIsMissing()
    {
        var request = CreateValidRequest("missing-api-key");

        using var client = _factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/smart-links", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Проверяет запрет создания умной ссылки с неверным API-ключом
    /// </summary>
    [Fact]
    public async Task CreateEndpointReturnsUnauthorizedWhenApiKeyIsInvalid()
    {
        var request = CreateValidRequest("invalid-api-key");

        using var client = CreateClientWithApiKey("invalid-api-key");
        using var response = await client.PostAsJsonAsync("/api/smart-links", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Проверяет создание и сохранение умной ссылки с корректным API-ключом
    /// </summary>
    [Fact]
    public async Task CreateEndpointCreatesAndPersistsSmartLinkWhenRequestIsValid()
    {
        var request = CreateValidRequest("created-smart-link");

        using var client = CreateClientWithApiKey(_apiKey);
        using var response = await client.PostAsJsonAsync("/api/smart-links", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var responseDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var id = responseDocument.RootElement.GetProperty("id").GetGuid();

        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal($"/api/smart-links/{id}", response.Headers.Location?.OriginalString);

        await using var context = CreateContext();
        var persistedSmartLink = await context.SmartLinks
            .AsNoTracking()
            .SingleOrDefaultAsync(smartLink => smartLink.Id == id);

        Assert.NotNull(persistedSmartLink);
        Assert.Equal(request.Slug, persistedSmartLink.Slug);
        Assert.Equal(request.DefaultUrl, persistedSmartLink.DefaultUrl);
        Assert.Equal(request.IsActive, persistedSmartLink.IsActive);
        Assert.Collection(
            persistedSmartLink.Rules,
            rule =>
            {
                Assert.Equal(10, rule.Priority);
                Assert.True(rule.IsEnabled);
                Assert.Equal("https://example.com/kazakhstan", rule.TargetUrl);
                Assert.Equal(CreateCountryDsl("KZ"), rule.ConditionDsl);
            },
            rule =>
            {
                Assert.Equal(20, rule.Priority);
                Assert.False(rule.IsEnabled);
                Assert.Equal("https://example.com/germany", rule.TargetUrl);
                Assert.Equal(CreateCountryDsl("DE"), rule.ConditionDsl);
            });
    }

    /// <summary>
    /// Проверяет конфликт при повторном коротком адресе без учёта регистра
    /// </summary>
    [Fact]
    public async Task CreateEndpointReturnsConflictWhenSlugAlreadyExistsIgnoringCase()
    {
        using var client = CreateClientWithApiKey(_apiKey);
        using var firstResponse = await client.PostAsJsonAsync(
            "/api/smart-links",
            CreateValidRequest("duplicate-smart-link"));

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using var secondResponse = await client.PostAsJsonAsync(
            "/api/smart-links",
            CreateValidRequest("DUPLICATE-SMART-LINK"));

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var problemDetails = await ReadProblemDetailsAsync(secondResponse);

        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
        Assert.Equal("Умная ссылка уже существует", problemDetails.Title);
    }

    /// <summary>
    /// Проверяет Problem Details при некорректном URL по умолчанию
    /// </summary>
    [Fact]
    public async Task CreateEndpointReturnsBadRequestWhenDefaultUrlIsInvalid()
    {
        var request = CreateValidRequest("invalid-default-url") with
        {
            DefaultUrl = "ftp://example.com/default"
        };

        using var client = CreateClientWithApiKey(_apiKey);
        using var response = await client.PostAsJsonAsync("/api/smart-links", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await ReadProblemDetailsAsync(response);

        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Equal("Некорректный запрос", problemDetails.Title);
        Assert.NotNull(problemDetails.Detail);
        Assert.Contains("URL по умолчанию", problemDetails.Detail);
    }

    /// <summary>
    /// Проверяет получение существующей умной ссылки без API-ключа
    /// </summary>
    [Fact]
    public async Task GetEndpointReturnsSmartLinkWhenSmartLinkExists()
    {
        var createRequest = CreateValidRequest("read-existing-smart-link");

        using var createClient = CreateClientWithApiKey(_apiKey);
        using var createResponse = await createClient.PostAsJsonAsync("/api/smart-links", createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateSmartLinkTestResponse>();

        Assert.NotNull(createResult);

        // Читающие операции Management API доступны без API-ключа
        using var getClient = _factory.CreateClient();
        using var getResponse = await getClient.GetAsync($"/api/smart-links/{createResult.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var smartLink = await getResponse.Content.ReadFromJsonAsync<GetSmartLinkTestResponse>();

        Assert.NotNull(smartLink);
        Assert.Equal(createResult.Id, smartLink.Id);
        Assert.Equal(createRequest.Slug, smartLink.Slug);
        Assert.Equal(createRequest.DefaultUrl, smartLink.DefaultUrl);
        Assert.Equal(createRequest.IsActive, smartLink.IsActive);
        Assert.Collection(
            smartLink.Rules,
            rule =>
            {
                Assert.Equal(10, rule.Priority);
                Assert.True(rule.IsEnabled);
                Assert.Equal("https://example.com/kazakhstan", rule.TargetUrl);
                Assert.Equal(CreateCountryDsl("KZ"), rule.ConditionDsl);
            },
            rule =>
            {
                Assert.Equal(20, rule.Priority);
                Assert.False(rule.IsEnabled);
                Assert.Equal("https://example.com/germany", rule.TargetUrl);
                Assert.Equal(CreateCountryDsl("DE"), rule.ConditionDsl);
            });
    }

    /// <summary>
    /// Проверяет Problem Details при чтении отсутствующей умной ссылки
    /// </summary>
    [Fact]
    public async Task GetEndpointReturnsNotFoundWhenSmartLinkDoesNotExist()
    {
        var id = Guid.NewGuid();

        // Читающие операции Management API доступны без API-ключа
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync($"/api/smart-links/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await ReadProblemDetailsAsync(response);

        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.Equal("Умная ссылка не найдена", problemDetails.Title);
        Assert.NotNull(problemDetails.Detail);
        Assert.Contains(id.ToString(), problemDetails.Detail);
    }

    /// <summary>
    /// Проверяет запрет изменения умной ссылки без API-ключа
    /// </summary>
    [Fact]
    public async Task UpdateEndpointReturnsUnauthorizedWhenApiKeyIsMissing()
    {
        var id = await CreateSmartLinkAsync("update-without-api-key");
        var request = CreateValidUpdateRequest("updated-without-api-key");

        using var client = _factory.CreateClient();
        using var response = await client.PutAsJsonAsync(
            $"/api/smart-links/{id}",
            request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Проверяет полную замену и сохранение конфигурации умной ссылки
    /// </summary>
    [Fact]
    public async Task UpdateEndpointReplacesAndPersistsSmartLinkWhenRequestIsValid()
    {
        var id = await CreateSmartLinkAsync("smart-link-before-update");
        var request = CreateValidUpdateRequest("smart-link-after-update");

        using var updateClient = CreateClientWithApiKey(_apiKey);
        using var updateResponse = await updateClient.PutAsJsonAsync(
            $"/api/smart-links/{id}",
            request);

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        // Проверяем результат через публичный HTTP-контракт чтения
        using var getClient = _factory.CreateClient();
        using var getResponse = await getClient.GetAsync($"/api/smart-links/{id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var smartLink =
            await getResponse.Content.ReadFromJsonAsync<GetSmartLinkTestResponse>();

        Assert.NotNull(smartLink);
        Assert.Equal(id, smartLink.Id);
        Assert.Equal(request.Slug, smartLink.Slug);
        Assert.Equal(request.DefaultUrl, smartLink.DefaultUrl);
        Assert.Equal(request.IsActive, smartLink.IsActive);
        Assert.Collection(
            smartLink.Rules,
            rule =>
            {
                Assert.Equal(5, rule.Priority);
                Assert.True(rule.IsEnabled);
                Assert.Equal("https://example.com/poland", rule.TargetUrl);
                Assert.Equal(CreateCountryDsl("PL"), rule.ConditionDsl);
            });
    }

    /// <summary>
    /// Проверяет Problem Details при изменении отсутствующей умной ссылки
    /// </summary>
    [Fact]
    public async Task UpdateEndpointReturnsNotFoundWhenSmartLinkDoesNotExist()
    {
        var id = Guid.NewGuid();
        var request = CreateValidUpdateRequest("update-missing-smart-link");

        using var client = CreateClientWithApiKey(_apiKey);
        using var response = await client.PutAsJsonAsync(
            $"/api/smart-links/{id}",
            request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await ReadProblemDetailsAsync(response);

        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.Equal("Умная ссылка не найдена", problemDetails.Title);
        Assert.NotNull(problemDetails.Detail);
        Assert.Contains(id.ToString(), problemDetails.Detail);
    }

    /// <summary>
    /// Проверяет конфликт при изменении slug на занятое значение без учёта регистра
    /// </summary>
    [Fact]
    public async Task UpdateEndpointReturnsConflictWhenSlugAlreadyExistsIgnoringCase()
    {
        await CreateSmartLinkAsync("existing-update-slug");
        var updatedSmartLinkId = await CreateSmartLinkAsync("changed-update-slug");
        var request = CreateValidUpdateRequest("EXISTING-UPDATE-SLUG");

        using var client = CreateClientWithApiKey(_apiKey);
        using var response = await client.PutAsJsonAsync(
            $"/api/smart-links/{updatedSmartLinkId}",
            request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problemDetails = await ReadProblemDetailsAsync(response);

        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
        Assert.Equal("Умная ссылка уже существует", problemDetails.Title);
    }

    /// <summary>
    /// Проверяет Problem Details при изменении ссылки с некорректным URL
    /// </summary>
    [Fact]
    public async Task UpdateEndpointReturnsBadRequestWhenDefaultUrlIsInvalid()
    {
        var id = await CreateSmartLinkAsync("invalid-update-url");
        var request = CreateValidUpdateRequest("invalid-update-url") with
        {
            DefaultUrl = "ftp://example.com/default"
        };

        using var client = CreateClientWithApiKey(_apiKey);
        using var response = await client.PutAsJsonAsync(
            $"/api/smart-links/{id}",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await ReadProblemDetailsAsync(response);

        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Equal("Некорректный запрос", problemDetails.Title);
        Assert.NotNull(problemDetails.Detail);
        Assert.Contains("URL по умолчанию", problemDetails.Detail);
    }

    /// <summary>
    /// Создаёт HTTP-клиент с указанным API-ключом
    /// </summary>
    private HttpClient CreateClientWithApiKey(string apiKey)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        return client;
    }

    /// <summary>
    /// Создаёт корректный запрос умной ссылки с двумя правилами
    /// </summary>
    private static CreateSmartLinkTestRequest CreateValidRequest(string slug)
    {
        return new CreateSmartLinkTestRequest(
            slug,
            "https://example.com/default",
            true,
            [
                new SmartLinkRuleTestRequest(
                    20,
                    false,
                    "https://example.com/germany",
                    CreateCountryDsl("DE")),
                new SmartLinkRuleTestRequest(
                    10,
                    true,
                    "https://example.com/kazakhstan",
                    CreateCountryDsl("KZ"))
            ]);
    }

    /// <summary>
    /// Создаёт умную ссылку через Management API и возвращает её идентификатор
    /// </summary>
    private async Task<Guid> CreateSmartLinkAsync(string slug)
    {
        using var client = CreateClientWithApiKey(_apiKey);
        using var response = await client.PostAsJsonAsync(
            "/api/smart-links",
            CreateValidRequest(slug));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<CreateSmartLinkTestResponse>();

        Assert.NotNull(result);

        return result.Id;
    }

    /// <summary>
    /// Создаёт корректный запрос полной замены умной ссылки
    /// </summary>
    private static UpdateSmartLinkTestRequest CreateValidUpdateRequest(string slug)
    {
        return new UpdateSmartLinkTestRequest(
            slug,
            "https://example.com/updated-default",
            false,
            [
                new SmartLinkRuleTestRequest(
                    5,
                    true,
                    "https://example.com/poland",
                    CreateCountryDsl("PL"))
            ]);
    }

    /// <summary>
    /// Создаёт DSL условия для указанной страны
    /// </summary>
    private static string CreateCountryDsl(string countryCode)
    {
        return $$"""
            {
              "dslVersion": 1,
              "condition": {
                "type": "country",
                "parameters": {
                  "countryCode": "{{countryCode}}"
                }
              }
            }
            """;
    }

    /// <summary>
    /// Читает Problem Details из HTTP-ответа
    /// </summary>
    private static async Task<ProblemDetails> ReadProblemDetailsAsync(HttpResponseMessage response)
    {
        return Assert.IsType<ProblemDetails>(await response.Content.ReadFromJsonAsync<ProblemDetails>());
    }

    /// <summary>
    /// Создаёт контекст тестовой базы PostgreSQL
    /// </summary>
    private ManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ManagementDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new ManagementDbContext(options);
    }

    /// <summary>
    /// Создаёт тестовый хост с настройками PostgreSQL и API-ключа
    /// </summary>
    private sealed class ManagementApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _apiKey;
        private readonly string _connectionString;

        /// <summary>
        /// Инициализирует фабрику тестового хоста Management API
        /// </summary>
        public ManagementApiFactory(string connectionString, string apiKey)
        {
            _connectionString = connectionString;
            _apiKey = apiKey;
        }

        /// <summary>
        /// Передаёт тестовому приложению тестовую конфигурацию
        /// </summary>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Management", _connectionString);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:ApiKey"] = _apiKey
                });
            });
        }
    }

    /// <summary>
    /// Описывает тестовый запрос создания умной ссылки
    /// </summary>
    private sealed record CreateSmartLinkTestRequest(
        string Slug,
        string DefaultUrl,
        bool IsActive,
        SmartLinkRuleTestRequest[] Rules);

    /// <summary>
    /// Описывает правило в тестовом HTTP-запросе
    /// </summary>
    private sealed record SmartLinkRuleTestRequest(
        int Priority,
        bool IsEnabled,
        string TargetUrl,
        string ConditionDsl);

    /// <summary>
    /// Описывает тестовый результат создания умной ссылки
    /// </summary>
    private sealed record CreateSmartLinkTestResponse(Guid Id);

    /// <summary>
    /// Описывает тестовый результат чтения умной ссылки
    /// </summary>
    private sealed record GetSmartLinkTestResponse(
        Guid Id,
        string Slug,
        string DefaultUrl,
        bool IsActive,
        SmartLinkRuleTestResponse[] Rules);

    /// <summary>
    /// Описывает правило в тестовом HTTP-ответе
    /// </summary>
    private sealed record SmartLinkRuleTestResponse(
        int Priority,
        bool IsEnabled,
        string TargetUrl,
        string ConditionDsl);

    /// <summary>
    /// Описывает тестовый запрос полной замены умной ссылки
    /// </summary>
    private sealed record UpdateSmartLinkTestRequest(
        string Slug,
        string DefaultUrl,
        bool IsActive,
        SmartLinkRuleTestRequest[] Rules);
}