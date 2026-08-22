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

    // Создаёт через DI контекст с текущим системным UTC-временем
    [Fact]
    public void AddSmartLinksRuleEngineCreatesContextWithSystemUtcTimeFeature()
    {
        var services = new ServiceCollection();

        services.AddSmartLinksRuleEngine();

        using var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<UrlResolutionContextFactory>();
        var beforeUtc = TimeProvider.System.GetUtcNow();

        var context = factory.Create(new UrlResolutionRequest());

        var afterUtc = TimeProvider.System.GetUtcNow();
        var feature = context.GetRequiredFeature<UtcTimeFeature>();

        Assert.InRange(feature.UtcNow, beforeUtc, afterUtc);
    }

    // Регистрирует фабрику условия страны
    [Fact]
    public void AddSmartLinksRuleEngineRegistersCountryConditionFactory()
    {
        var services = new ServiceCollection();

        services.AddSmartLinksRuleEngine();

        using var serviceProvider = services.BuildServiceProvider();

        Assert.Contains(
            serviceProvider.GetServices<IConditionFactory>(),
            conditionFactory => conditionFactory is CountryConditionFactory);
    }

    // Регистрирует contributor текущего UTC-времени
    [Fact]
    public void AddSmartLinksRuleEngineRegistersUtcTimeContextContributor()
    {
        var services = new ServiceCollection();

        services.AddSmartLinksRuleEngine();

        using var serviceProvider = services.BuildServiceProvider();
        var contributors = serviceProvider.GetServices<IResolutionContextContributor>();

        Assert.Contains(contributors, contributor => contributor is UtcTimeContextContributor);
    }

    // Регистрирует фабрику условия устройства
    [Fact]
    public void AddSmartLinksRuleEngineRegistersDeviceConditionFactory()
    {
        var services = new ServiceCollection();

        services.AddSmartLinksRuleEngine();

        using var serviceProvider = services.BuildServiceProvider();
        var conditionFactories = serviceProvider.GetServices<IConditionFactory>();

        Assert.Contains(conditionFactories, conditionFactory => conditionFactory is DeviceConditionFactory);
    }

    // Создаёт через DI признаки устройства и браузера из User-Agent
    [Fact]
    public void AddSmartLinksRuleEngineCreatesContextWithUserAgentFeatures()
    {
        const string userAgent = "Mozilla/5.0 (Linux; Android 14; Pixel 8) Chrome/126 Mobile Safari EdgA/126";
        var services = new ServiceCollection();

        services.AddSmartLinksRuleEngine();

        using var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<UrlResolutionContextFactory>();

        var context = factory.Create(new UrlResolutionRequest(UserAgent: userAgent));
        var deviceFeature = context.GetRequiredFeature<DeviceFeature>();
        var browserFeature = context.GetRequiredFeature<BrowserFeature>();

        Assert.Equal("mobile", deviceFeature.DeviceType);
        Assert.Equal("edge", browserFeature.Browser);
    }
}