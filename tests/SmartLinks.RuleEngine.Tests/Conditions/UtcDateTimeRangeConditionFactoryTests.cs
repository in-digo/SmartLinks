using System.Text.Json;
using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Conditions;

public sealed class UtcDateTimeRangeConditionFactoryTests
{
    // Создаёт условие с временным интервалом из JSON-параметров
    [Fact]
    public void CreateReturnsConditionUsingConfiguredUtcRange()
    {
        const string parametersJson = """
        {
            "fromUtc": "2026-08-19T10:00:00+00:00",
            "toUtc": "2026-08-19T12:00:00+00:00"
        }
        """;

        using var document = JsonDocument.Parse(parametersJson);

        var factory = new UtcDateTimeRangeConditionFactory();
        var condition = factory.Create(document.RootElement);
        var requestTimeUtc = new DateTimeOffset(2026, 8, 19, 11, 0, 0, TimeSpan.Zero);
        var utcTimeFeature = new UtcTimeFeature(requestTimeUtc);
        var context = new UrlResolutionContext(new IResolutionFeature[] { utcTimeFeature });
        
        var result = condition.IsMatch(context);

        Assert.True(result);
    }

    // Возвращает имя временного предиката из DSL
    [Fact]
    public void TypeReturnsUtcTimePredicateName()
    {
        var factory = new UtcDateTimeRangeConditionFactory();

        Assert.Equal("utcTime", factory.Type);
    }
}