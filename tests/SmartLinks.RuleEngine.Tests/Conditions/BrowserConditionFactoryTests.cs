using System.Text.Json;
using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Conditions;

public sealed class BrowserConditionFactoryTests
{
    // Создаёт условие браузера из JSON-параметров
    [Fact]
    public void CreateReturnsConditionUsingConfiguredBrowser()
    {
        const string parametersJson = """
        {
            "browser": "chrome"
        }
        """;

        using var document = JsonDocument.Parse(parametersJson);

        var factory = new BrowserConditionFactory();
        var condition = factory.Create(document.RootElement);
        var browserFeature = new BrowserFeature("Chrome");
        var context = new UrlResolutionContext(new IResolutionFeature[] { browserFeature });

        var result = condition.IsMatch(context);

        Assert.True(result);
    }

    // Возвращает имя предиката браузера из DSL
    [Fact]
    public void TypeReturnsBrowserPredicateName()
    {
        var factory = new BrowserConditionFactory();

        Assert.Equal("browser", factory.Type);
    }
}