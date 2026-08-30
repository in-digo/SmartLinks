using Microsoft.Extensions.DependencyInjection;
using SmartLinks.Redirect.Infrastructure.DependencyInjection;
using SmartLinks.Redirect.Infrastructure.Management;
using System.Net;
using System.Text;
using Microsoft.Extensions.Http;

namespace SmartLinks.Redirect.UnitTests.DependencyInjection;

public sealed class RedirectInfrastructureServiceCollectionExtensionsTests
{
    /// <summary>
    /// Проверяет регистрацию typed HTTP-клиента Management
    /// </summary>
    [Fact]
    public void AddRedirectInfrastructureRegistersTypedManagementConfigurationClient()
    {
        var services = new ServiceCollection();

        services.AddRedirectInfrastructure(new Uri("https://management.test"));

        using var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IManagementConfigurationClient>();

        Assert.IsType<ManagementConfigurationClient>(client);
    }

    /// <summary>
    /// Проверяет настройку базового адреса Management API
    /// </summary>
    [Fact]
    public async Task AddRedirectInfrastructureConfiguresManagementApiBaseAddress()
    {
        var services = new ServiceCollection();
        using var handler = new StubHttpMessageHandler();

        services.ConfigureAll<HttpClientFactoryOptions>(
            options => options.HttpMessageHandlerBuilderActions.Add(
                builder => builder.PrimaryHandler = handler));
        services.AddRedirectInfrastructure(new Uri("https://management.test"));

        using var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IManagementConfigurationClient>();

        await client.GetSnapshotAsync(CancellationToken.None);

        Assert.Equal(new Uri("https://management.test/internal/configurations/snapshot"), handler.RequestUri);
    }

    /// <summary>
    /// Проверяет ошибку регистрации инфраструктуры без коллекции сервисов
    /// </summary>
    [Fact]
    public void AddRedirectInfrastructureWithNullServicesThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var exception = Assert.Throws<ArgumentNullException>(
            () => services.AddRedirectInfrastructure(new Uri("https://management.test")));

        Assert.Equal("services", exception.ParamName);
    }

    /// <summary>
    /// Проверяет ошибку регистрации без базового адреса Management API
    /// </summary>
    [Fact]
    public void AddRedirectInfrastructureWithNullBaseAddressThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Uri managementApiBaseAddress = null!;

        var exception = Assert.Throws<ArgumentNullException>(
            () => services.AddRedirectInfrastructure(managementApiBaseAddress));

        Assert.Equal("managementApiBaseAddress", exception.ParamName);
    }

    /// <summary>
    /// Проверяет отклонение относительного базового адреса Management API
    /// </summary>
    [Fact]
    public void AddRedirectInfrastructureWithRelativeBaseAddressThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var managementApiBaseAddress = new Uri("/management", UriKind.Relative);

        var exception = Assert.Throws<ArgumentException>(
            () => services.AddRedirectInfrastructure(managementApiBaseAddress));

        Assert.Equal("managementApiBaseAddress", exception.ParamName);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        /// <summary>
        /// Запоминает HTTP-запрос и возвращает пустой snapshot
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RequestUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                    "revision": 0,
                    "configurations": []
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            });
        }
    }
}