using System.Net;
using System.Text;
using SmartLinks.Redirect.Infrastructure.Management;

namespace SmartLinks.Redirect.UnitTests.Infrastructure.Management;

public sealed class ManagementConfigurationClientTests
{
    /// <summary>
    /// Проверяет запрос и десериализацию полного snapshot из Management API
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsyncRequestsSnapshotAndReturnsDeserializedResponse()
    {
        var configurationId = Guid.Parse("c9269e6f-ad50-43d8-adcf-9749bc824ef4");
        var responseJson = $$"""
            {
              "revision": 7,
              "configurations": [
                {
                  "id": "{{configurationId}}",
                  "slug": "summer-sale",
                  "defaultUrl": "https://example.com/default",
                  "isActive": true,
                  "rules": [
                    {
                      "priority": 10,
                      "isEnabled": true,
                      "targetUrl": "https://example.com/target",
                      "conditionDsl": "{}"
                    }
                  ]
                }
              ]
            }
            """;

        using var handler = new StubHttpMessageHandler(responseJson);
        using var httpClient = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://management.test")
        };
        var client = new ManagementConfigurationClient(httpClient);

        var result = await client.GetSnapshotAsync(CancellationToken.None);

        Assert.Equal(7, result.Revision);

        var configuration = Assert.Single(result.Configurations);
        Assert.Equal(configurationId, configuration.Id);
        Assert.Equal("summer-sale", configuration.Slug);
        Assert.Equal("https://example.com/default", configuration.DefaultUrl);
        Assert.True(configuration.IsActive);

        var rule = Assert.Single(configuration.Rules);
        Assert.Equal(10, rule.Priority);
        Assert.True(rule.IsEnabled);
        Assert.Equal("https://example.com/target", rule.TargetUrl);
        Assert.Equal("{}", rule.ConditionDsl);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal("/internal/configurations/snapshot", handler.RequestUri?.PathAndQuery);
    }

    /// <summary>
    /// Проверяет ошибку создания клиента без HTTP-подключения
    /// </summary>
    [Fact]
    public void ConstructorWithNullHttpClientThrowsArgumentNullException()
    {
        HttpClient httpClient = null!;

        Assert.Throws<ArgumentNullException>(() => new ManagementConfigurationClient(httpClient));
    }

    /// <summary>
    /// Проверяет ошибку создания клиента без базового адреса Management API
    /// </summary>
    [Fact]
    public void ConstructorWithoutBaseAddressThrowsInvalidOperationException()
    {
        using var httpClient = new HttpClient();

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ManagementConfigurationClient(httpClient));

        Assert.Equal("Не задан базовый адрес Management API", exception.Message);
    }

    /// <summary>
    /// Проверяет ошибку получения snapshot при неуспешном ответе Management API
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsyncWithUnsuccessfulResponseThrowsHttpRequestException()
    {
        using var handler = new StubHttpMessageHandler(
            "{}",
            HttpStatusCode.ServiceUnavailable);
        using var httpClient = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://management.test")
        };
        var client = new ManagementConfigurationClient(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetSnapshotAsync(CancellationToken.None));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    /// <summary>
    /// Проверяет ошибку получения snapshot при пустом JSON-результате
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsyncWithNullResponseThrowsInvalidOperationException()
    {
        using var handler = new StubHttpMessageHandler("null");
        using var httpClient = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://management.test")
        };
        var client = new ManagementConfigurationClient(httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetSnapshotAsync(CancellationToken.None));

        Assert.Equal("Management API вернул пустой snapshot", exception.Message);
    }

    /// <summary>
    /// Проверяет передачу отмены в HTTP-запрос полного snapshot
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsyncPropagatesCancellationToHttpRequest()
    {
        using var handler = new WaitingHttpMessageHandler();
        using var httpClient = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://management.test")
        };
        var client = new ManagementConfigurationClient(httpClient);
        using var cancellationTokenSource = new CancellationTokenSource();

        var requestTask = client.GetSnapshotAsync(cancellationTokenSource.Token);
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await requestTask);
    }

    /// <summary>
    /// Проверяет запрос и десериализацию изменений после указанной ревизии
    /// </summary>
    [Fact]
    public async Task GetChangesAsyncRequestsChangesAfterRevisionAndReturnsDeserializedResponse()
    {
        var configurationId = Guid.Parse("d4e87b1e-999b-4d3a-82d5-8c02e3a44306");
        var responseJson = $$"""
            [
            {
                "revision": 8,
                "configuration": {
                "id": "{{configurationId}}",
                "slug": "autumn-sale",
                "defaultUrl": "https://example.com/default",
                "isActive": true,
                "rules": [
                    {
                    "priority": 20,
                    "isEnabled": true,
                    "targetUrl": "https://example.com/target",
                    "conditionDsl": "{}"
                    }
                ]
                }
            }
            ]
            """;

        using var handler = new StubHttpMessageHandler(responseJson);
        using var httpClient = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://management.test")
        };
        var client = new ManagementConfigurationClient(httpClient);

        var result = await client.GetChangesAsync(
            afterRevision: 7,
            limit: 50,
            CancellationToken.None);

        var change = Assert.Single(result);
        Assert.Equal(8, change.Revision);
        Assert.Equal(configurationId, change.Configuration.Id);
        Assert.Equal("autumn-sale", change.Configuration.Slug);
        Assert.Equal("https://example.com/default", change.Configuration.DefaultUrl);
        Assert.True(change.Configuration.IsActive);

        var rule = Assert.Single(change.Configuration.Rules);
        Assert.Equal(20, rule.Priority);
        Assert.True(rule.IsEnabled);
        Assert.Equal("https://example.com/target", rule.TargetUrl);
        Assert.Equal("{}", rule.ConditionDsl);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal("/internal/configurations/changes?afterRevision=7&limit=50", handler.RequestUri?.PathAndQuery);
    }

    /// <summary>
    /// Проверяет пустой результат при отсутствии новых изменений
    /// </summary>
    [Fact]
    public async Task GetChangesAsyncReturnsEmptyCollectionWhenNoChangesAreAvailable()
    {
        using var handler = new StubHttpMessageHandler("[]");
        using var httpClient = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://management.test")
        };
        var client = new ManagementConfigurationClient(httpClient);

        var result = await client.GetChangesAsync(
            afterRevision: 12,
            limit: 100,
            CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal("/internal/configurations/changes?afterRevision=12&limit=100", handler.RequestUri?.PathAndQuery);
    }

    /// <summary>
    /// Проверяет отклонение недопустимых параметров change feed до HTTP-запроса
    /// </summary>
    [Theory]
    [InlineData(-1, 1, "afterRevision")]
    [InlineData(0, 0, "limit")]
    [InlineData(0, -1, "limit")]
    public async Task GetChangesAsyncWithInvalidArgumentsThrowsArgumentOutOfRangeException(
        long afterRevision,
        int limit,
        string parameterName)
    {
        using var handler = new StubHttpMessageHandler("[]");
        using var httpClient = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://management.test")
        };
        var client = new ManagementConfigurationClient(httpClient);

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.GetChangesAsync(
                afterRevision,
                limit,
                CancellationToken.None));

        Assert.Equal(parameterName, exception.ParamName);
        Assert.Null(handler.RequestUri);
    }

    /// <summary>
    /// Проверяет ошибку получения изменений при неуспешном ответе Management API
    /// </summary>
    [Fact]
    public async Task GetChangesAsyncWithUnsuccessfulResponseThrowsHttpRequestException()
    {
        using var handler = new StubHttpMessageHandler(
            "[]",
            HttpStatusCode.ServiceUnavailable);
        using var httpClient = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://management.test")
        };
        var client = new ManagementConfigurationClient(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetChangesAsync(
                afterRevision: 7,
                limit: 50,
                CancellationToken.None));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    /// <summary>
    /// Проверяет ошибку получения изменений при пустом JSON-результате
    /// </summary>
    [Fact]
    public async Task GetChangesAsyncWithNullResponseThrowsInvalidOperationException()
    {
        using var handler = new StubHttpMessageHandler("null");
        using var httpClient = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://management.test")
        };
        var client = new ManagementConfigurationClient(httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetChangesAsync(
                afterRevision: 7,
                limit: 50,
                CancellationToken.None));

        Assert.Equal("Management API вернул пустой список изменений", exception.Message);
    }

    /// <summary>
    /// Проверяет передачу отмены в HTTP-запрос изменений
    /// </summary>
    [Fact]
    public async Task GetChangesAsyncPropagatesCancellationToHttpRequest()
    {
        using var handler = new WaitingHttpMessageHandler();
        using var httpClient = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://management.test")
        };
        var client = new ManagementConfigurationClient(httpClient);
        using var cancellationTokenSource = new CancellationTokenSource();

        var requestTask = client.GetChangesAsync(
            afterRevision: 7,
            limit: 50,
            cancellationTokenSource.Token);
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await requestTask);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseJson;
        private readonly HttpStatusCode _statusCode;

        /// <summary>
        /// Инициализирует обработчик заданным HTTP-ответом
        /// </summary>
        public StubHttpMessageHandler(
            string responseJson,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseJson = responseJson;
            _statusCode = statusCode;
        }

        public HttpMethod? RequestMethod { get; private set; }

        public Uri? RequestUri { get; private set; }

        /// <summary>
        /// Запоминает HTTP-запрос и возвращает подготовленный JSON
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RequestMethod = request.Method;
            RequestUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class WaitingHttpMessageHandler : HttpMessageHandler
    {
        /// <summary>
        /// Ожидает отмены выполняемого HTTP-запроса
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}