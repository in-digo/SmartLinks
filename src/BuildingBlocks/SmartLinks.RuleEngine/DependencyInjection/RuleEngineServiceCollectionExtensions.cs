using Microsoft.Extensions.DependencyInjection;
using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.DependencyInjection;

/// <summary>
/// Содержит регистрацию зависимостей движка правил
/// </summary>
public static class RuleEngineServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует сервисы и встроенные условия движка правил
    /// </summary>
    public static IServiceCollection AddSmartLinksRuleEngine(this IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(TimeProvider.System);

        services.AddSingleton<IClientDeviceResolver, UserAgentDeviceResolver>();
        services.AddSingleton<IClientBrowserResolver, UserAgentBrowserResolver>();

        services.AddSingleton<IResolutionContextContributor, UtcTimeContextContributor>();
        services.AddSingleton<IResolutionContextContributor, DeviceContextContributor>();
        services.AddSingleton<IResolutionContextContributor, BrowserContextContributor>();

        services.AddSingleton<UrlResolutionContextFactory>();
        services.AddSingleton<ISmartLinkResolver, SmartLinkResolver>();

        services.AddSingleton<IConditionFactory, UtcDateTimeRangeConditionFactory>();
        services.AddSingleton<IConditionFactory, CountryConditionFactory>();
        services.AddSingleton<IConditionFactory, DeviceConditionFactory>();

        services.AddSingleton<ConditionCompiler>();

        return services;
    }
}