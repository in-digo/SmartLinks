using Microsoft.Extensions.DependencyInjection;
using SmartLinks.Redirect.Application.Configurations;
using SmartLinks.RuleEngine.DependencyInjection;

namespace SmartLinks.Redirect.Application.DependencyInjection;

/// <summary>
/// Содержит регистрацию зависимостей application-слоя Redirect
/// </summary>
public static class RedirectApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует единственное хранилище snapshot для чтения и обновления
    /// </summary>
    public static IServiceCollection AddRedirectApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSmartLinksRuleEngine();
        services.AddSingleton<ConfigurationSnapshotStore>();

        services.AddSingleton<IConfigurationSnapshotProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<ConfigurationSnapshotStore>());

        services.AddSingleton<IConfigurationSnapshotUpdater>(serviceProvider =>
            serviceProvider.GetRequiredService<ConfigurationSnapshotStore>());

        return services;
    }
}