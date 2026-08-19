using Microsoft.Extensions.DependencyInjection;
using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.DependencyInjection;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.DependencyInjection;

public sealed class RuleEngineServiceCollectionExtensionsTests
{
    // Регистрирует обязательные сервисы движка правил
    [Fact]
    public void AddSmartLinksRuleEngineRegistersRequiredServices()
    {
        var services = new ServiceCollection();

        services.AddSmartLinksRuleEngine();

        using var serviceProvider = services.BuildServiceProvider();

        Assert.IsType<SmartLinkResolver>(
            serviceProvider.GetRequiredService<ISmartLinkResolver>());

        Assert.IsType<ConditionCompiler>(
            serviceProvider.GetRequiredService<ConditionCompiler>());

        Assert.Contains(
            serviceProvider.GetServices<IConditionFactory>(),
            conditionFactory => conditionFactory is UtcDateTimeRangeConditionFactory);
    }
}