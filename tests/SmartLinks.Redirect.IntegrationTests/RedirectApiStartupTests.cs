using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SmartLinks.Redirect.Infrastructure.Management;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SmartLinks.Redirect.Infrastructure.Synchronization;

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

    /// <summary>
    /// Проверяет регистрацию клиента Management API в Redirect API
    /// </summary>
    [Fact]
    public void RedirectApiRegistersManagementConfigurationClient()
    {
        var client = _factory.Services.GetRequiredService<IManagementConfigurationClient>();

        Assert.IsType<ManagementConfigurationClient>(client);
    }

    /// <summary>
    /// Проверяет привязку настроек фоновой синхронизации из конфигурации Redirect API
    /// </summary>
    [Fact]
    public void RedirectApiBindsConfigurationSynchronizationOptions()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ManagementApi:BaseAddress", "https://management.test");
            builder.UseSetting("ConfigurationSynchronization:PollingInterval", "00:00:17");
            builder.UseSetting("ConfigurationSynchronization:ChangeBatchSize", "25");
            builder.UseSetting("ConfigurationSynchronization:InitialRetryDelay", "00:00:03");
            builder.UseSetting("ConfigurationSynchronization:MaximumRetryDelay", "00:00:19");
        });

        var options = factory.Services
            .GetRequiredService<IOptions<ConfigurationSynchronizationOptions>>()
            .Value;

        Assert.Equal(TimeSpan.FromSeconds(17), options.PollingInterval);
        Assert.Equal(25, options.ChangeBatchSize);
        Assert.Equal(TimeSpan.FromSeconds(3), options.InitialRetryDelay);
        Assert.Equal(TimeSpan.FromSeconds(19), options.MaximumRetryDelay);
    }

    /// <summary>
    /// Проверяет регистрацию фонового worker синхронизации в Redirect API
    /// </summary>
    [Fact]
    public void RedirectApiRegistersConfigurationSynchronizationWorker()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting( "ManagementApi:BaseAddress", "https://management.test");
        });

        var hostedServices = factory.Services.GetServices<IHostedService>();

        Assert.Contains(hostedServices, hostedService => hostedService is ConfigurationSynchronizationWorker);
    }

    /// <summary>
    /// Проверяет ошибку запуска Redirect API без базового адреса Management API
    /// </summary>
    [Fact]
    public void RedirectApiWithoutManagementApiBaseAddressFailsToStart()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ManagementApi:BaseAddress", string.Empty);
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var client = factory.CreateClient();
        });

        Assert.Equal("Не задан базовый адрес Management API", exception.Message);
    }

    /// <summary>
    /// Проверяет ошибку запуска Redirect API с некорректным адресом Management API
    /// </summary>
    [Fact]
    public void RedirectApiWithInvalidManagementApiBaseAddressFailsToStart()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ManagementApi:BaseAddress", "relative-management-address");
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var client = factory.CreateClient();
        });

        Assert.Equal("Задан некорректный базовый адрес Management API", exception.Message);
    }
}