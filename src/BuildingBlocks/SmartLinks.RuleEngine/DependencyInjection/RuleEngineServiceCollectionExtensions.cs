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
        services.AddSingleton<UrlResolutionContextFactory>();
        services.AddSingleton<ISmartLinkResolver, SmartLinkResolver>();
        services.AddSingleton<IConditionFactory, UtcDateTimeRangeConditionFactory>();
        services.AddSingleton<ConditionCompiler>();

        return services;
    }
}