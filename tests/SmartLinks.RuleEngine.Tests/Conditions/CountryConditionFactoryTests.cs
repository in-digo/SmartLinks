using System.Text.Json;
using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Conditions;

public sealed class CountryConditionFactoryTests
{
    // Создаёт условие страны из JSON-параметров
    [Fact]
    public void CreateReturnsConditionUsingConfiguredCountryCode()
    {
        const string parametersJson = """
        {
            "countryCode": "RU"
        }
        """;

        using var document = JsonDocument.Parse(parametersJson);

        var factory = new CountryConditionFactory();
        var condition = factory.Create(document.RootElement);
        var countryFeature = new CountryFeature("ru");
        var context = new UrlResolutionContext(new IResolutionFeature[] { countryFeature });

        var result = condition.IsMatch(context);

        Assert.True(result);
    }
}