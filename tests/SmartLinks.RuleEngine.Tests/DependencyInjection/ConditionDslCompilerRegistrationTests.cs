using Microsoft.Extensions.DependencyInjection;
using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.DependencyInjection;

namespace SmartLinks.RuleEngine.Tests.DependencyInjection;

public sealed class ConditionDslCompilerRegistrationTests
{
    /// <summary>
    /// Проверяет регистрацию DSL-компилятора в контейнере движка правил
    /// </summary>
    [Fact]
    public void AddSmartLinksRuleEngineRegistersConditionDslCompiler()
    {
        var services = new ServiceCollection();
        services.AddSmartLinksRuleEngine();

        using var serviceProvider = services.BuildServiceProvider();

        var compiler = serviceProvider.GetService<ConditionDslCompiler>();

        Assert.NotNull(compiler);
    }
}