using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartLinks.Redirect.Infrastructure.Management;
using SmartLinks.Redirect.Infrastructure.Synchronization;
using SmartLinks.Redirect.Infrastructure.GeoIp;
using SmartLinks.RuleEngine.Resolution;

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
        services.AddSingleton<ConfigurationSynchronizationState>();
        services.AddHostedService<ConfigurationSynchronizationWorker>();

        return services;
    }

    /// <summary>
    /// Регистрирует определитель страны по локальной базе MaxMind
    /// </summary>
    public static IServiceCollection AddMaxMindGeoIp(this IServiceCollection services, string databasePath)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        services.Replace(ServiceDescriptor.Singleton<IClientLocationResolver>(
            _ => new MaxMindClientLocationResolver(databasePath)));

        return services;
    }
}