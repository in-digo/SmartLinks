using Microsoft.Extensions.DependencyInjection;
using SmartLinks.Redirect.Infrastructure.Management;
using SmartLinks.Redirect.Infrastructure.Synchronization;

namespace SmartLinks.Redirect.Infrastructure.DependencyInjection;

/// <summary>
/// Содержит регистрацию инфраструктуры Redirect
/// </summary>
public static class RedirectInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует HTTP-доступ Redirect к Management API
    /// </summary>
    public static IServiceCollection AddRedirectInfrastructure(this IServiceCollection services, Uri managementApiBaseAddress)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(managementApiBaseAddress);

        if (!managementApiBaseAddress.IsAbsoluteUri)
            throw new ArgumentException("Базовый адрес Management API должен быть абсолютным", nameof(managementApiBaseAddress));

        services.AddHttpClient<IManagementConfigurationClient, ManagementConfigurationClient>(
            httpClient => httpClient.BaseAddress = managementApiBaseAddress);

        services.AddSingleton<ConfigurationSynchronizer>();
        services.AddOptions<ConfigurationSynchronizationOptions>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Random.Shared);
        services.AddSingleton<ConfigurationSynchronizationRetryDelayProvider>();
        services.AddHostedService<ConfigurationSynchronizationWorker>();

        return services;
    }
}