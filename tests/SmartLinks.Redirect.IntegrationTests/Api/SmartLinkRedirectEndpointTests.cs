using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartLinks.Contracts.Configurations;
using SmartLinks.Redirect.Application.Configurations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;

namespace SmartLinks.Redirect.IntegrationTests.Api;

public sealed class SmartLinkRedirectEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Сохраняет фабрику тестового хоста Redirect API
    /// </summary>
    public SmartLinkRedirectEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Проверяет временный редирект известной активной ссылки на URL по умолчанию
    /// </summary>
    [Fact]
    public async Task KnownActiveSmartLinkReturnsTemporaryRedirect()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        using var client = CreateClient(factory, CreateConfiguration());

        using var response = await client.GetAsync("/summer-sale");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("https://example.com/default", response.Headers.Location?.AbsoluteUri);
    }

    /// <summary>
    /// Проверяет запрет кэширования успешного редиректа
    /// </summary>
    [Fact]
    public async Task SuccessfulRedirectDisablesResponseCaching()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        using var client = CreateClient(factory, CreateConfiguration());

        using var response = await client.GetAsync("/summer-sale");

        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    /// <summary>
    /// Проверяет использование User-Agent при выборе подходящего правила
    /// </summary>
    [Fact]
    public async Task RedirectUsesUserAgentWhenResolvingRules()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        using var client = CreateClient(factory, CreateConfiguration(rules: [CreateMobileRule()]));
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_5) AppleWebKit Mobile Safari");

        using var response = await client.GetAsync("/summer-sale");

        Assert.Equal("https://example.com/mobile", response.Headers.Location?.AbsoluteUri);
    }

    /// <summary>
    /// Проверяет ответ 404 для неизвестного slug
    /// </summary>
    [Fact]
    public async Task UnknownSmartLinkReturnsNotFound()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/missing-link");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Проверяет ответ 404 для выключенной умной ссылки
    /// </summary>
    [Fact]
    public async Task InactiveSmartLinkReturnsNotFound()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        using var client = CreateClient(factory, CreateConfiguration(isActive: false));

        using var response = await client.GetAsync("/summer-sale");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Проверяет передачу не-GET запроса следующему обработчику
    /// </summary>
    [Fact]
    public async Task NonGetRequestIsNotHandledAsSmartLink()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        using var client = CreateClient(factory, CreateConfiguration());
        using var request = new HttpRequestMessage(HttpMethod.Post, "/summer-sale");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Проверяет передачу вложенного маршрута следующему обработчику
    /// </summary>
    [Fact]
    public async Task NestedPathIsNotHandledAsSmartLink()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        using var client = CreateClient(factory, CreateConfiguration(slug: "summer-sale/details"));

        using var response = await client.GetAsync("/summer-sale/details");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Проверяет доступность health endpoints при конфликтующих конфигурациях ссылок
    /// </summary>
    [Theory]
    [InlineData("/health/live", HttpStatusCode.OK)]
    [InlineData("/health/ready", HttpStatusCode.ServiceUnavailable)]
    public async Task HealthEndpointsAreNotHandledAsSmartLinks(string path, HttpStatusCode expectedStatusCode)
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        using var client = CreateClient(factory, CreateConfiguration(slug: path.TrimStart('/')));

        using var response = await client.GetAsync(path);

        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    /// <summary>
    /// Проверяет исключение маршрута Swagger из обработки умных ссылок
    /// </summary>
    [Fact]
    public async Task SwaggerRouteIsNotHandledAsSmartLink()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        using var client = CreateClient(factory, CreateConfiguration(slug: "swagger"));

        using var response = await client.GetAsync("/swagger");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Проверяет использование IP-адреса клиента из forwarded-заголовка
    /// </summary>
    [Fact]
    public async Task RedirectUsesForwardedClientIpAddressWhenResolvingCountryRule()
    {
        var databasePath = Path.Combine(AppContext.BaseDirectory, "TestData", "GeoIP2-Country-Test.mmdb");
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("GeoIp:DatabasePath", databasePath);
            builder.ConfigureServices(services =>
            {
                // В тестовом хосте разрешаем обработку заголовка от TestServer
                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                });
            });
        });
        using var client = CreateClient(factory, CreateConfiguration(rules: [CreateCountryRule()]));
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-For", "81.2.69.160");

        using var response = await client.GetAsync("/summer-sale");

        Assert.Equal("https://example.com/gb", response.Headers.Location?.AbsoluteUri);
    }

    /// <summary>
    /// Проверяет игнорирование клиентского IP от недоверенного reverse proxy
    /// </summary>
    [Fact]
    public async Task RedirectIgnoresForwardedClientIpAddressFromUntrustedProxy()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("GeoIp:DatabasePath", Path.Combine(AppContext.BaseDirectory, "TestData", "GeoIP2-Country-Test.mmdb"));
            builder.ConfigureServices(services =>
            {
                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                    options.KnownProxies.Add(IPAddress.Parse("192.0.2.1"));
                });
            });
        });
        using var client = CreateClient(factory, CreateConfiguration(rules: [CreateCountryRule()]));

        var responseContext = await factory.Server.SendAsync(requestContext =>
        {
            requestContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.25");
            requestContext.Request.Method = HttpMethod.Get.Method;
            requestContext.Request.Path = "/summer-sale";
            requestContext.Request.Headers["X-Forwarded-For"] = "81.2.69.160";
        });

        Assert.Equal((int)HttpStatusCode.Found, responseContext.Response.StatusCode);
        Assert.Equal("https://example.com/default", responseContext.Response.Headers.Location.ToString());
    }

    /// <summary>
    /// Проверяет обработку только ближайшего адреса из forwarded-заголовка
    /// </summary>
    [Fact]
    public async Task RedirectUsesNearestForwardedClientIpAddress()
    {
        var databasePath = Path.Combine(AppContext.BaseDirectory, "TestData", "GeoIP2-Country-Test.mmdb");
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("GeoIp:DatabasePath", databasePath);
            builder.ConfigureServices(services =>
            {
                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                });
            });
        });
        using var client = CreateClient(factory, CreateConfiguration(rules: [CreateCountryRule()]));
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-For", "203.0.113.10, 81.2.69.160");

        using var response = await client.GetAsync("/summer-sale");

        Assert.Equal("https://example.com/gb", response.Headers.Location?.AbsoluteUri);
    }

    /// <summary>
    /// Создаёт HTTP-клиент без автоматического перехода по редиректу и заполняет snapshot
    /// </summary>
    private static HttpClient CreateClient(
        WebApplicationFactory<Program> factory,
        params SmartLinkConfigurationSnapshot[] configurations)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var snapshotUpdater = factory.Services.GetRequiredService<IConfigurationSnapshotUpdater>();
        snapshotUpdater.ReplaceSnapshot(new PublishedSmartLinksSnapshot(1, configurations));

        return client;
    }

    /// <summary>
    /// Создаёт опубликованную конфигурацию умной ссылки
    /// </summary>
    private static SmartLinkConfigurationSnapshot CreateConfiguration(
        string slug = "summer-sale",
        string defaultUrl = "https://example.com/default",
        bool isActive = true,
        IReadOnlyList<SmartLinkRuleSnapshot>? rules = null)
    {
        return new SmartLinkConfigurationSnapshot(
            Guid.NewGuid(),
            slug,
            defaultUrl,
            isActive,
            rules ?? []);
    }

    /// <summary>
    /// Создаёт правило перенаправления мобильного клиента
    /// </summary>
    private static SmartLinkRuleSnapshot CreateMobileRule()
    {
        return new SmartLinkRuleSnapshot(
            10,
            true,
            "https://example.com/mobile",
            """
            {
              "dslVersion": 1,
              "condition": {
                "type": "device",
                "parameters": {
                  "deviceType": "mobile"
                }
              }
            }
            """);
    }

    /// <summary>
    /// Создаёт правило перенаправления клиента из Великобритании
    /// </summary>
    private static SmartLinkRuleSnapshot CreateCountryRule()
    {
        return new SmartLinkRuleSnapshot(
            10,
            true,
            "https://example.com/gb",
            """
            {
            "dslVersion": 1,
            "condition": {
                "type": "country",
                "parameters": {
                "countryCode": "GB"
                }
            }
            }
            """);
    }
}