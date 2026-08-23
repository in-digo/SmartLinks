using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Conditions;

public class NotConditionTests
{
    /// <summary>
    /// Проверяет отрицательный результат, если вложенное условие выполняется
    /// </summary>
    [Fact]
    public void IsMatchReturnsFalseWhenConditionMatches()
    {
        var condition = new NotCondition(new StubCondition(true));
        var context = new UrlResolutionContext(Array.Empty<IResolutionFeature>());

        var result = condition.IsMatch(context);

        Assert.False(result);
    }

    /// <summary>
    /// Проверяет положительный результат, если вложенное условие не выполняется
    /// </summary>
    [Fact]
    public void IsMatchReturnsTrueWhenConditionDoesNotMatch()
    {
        var condition = new NotCondition(new StubCondition(false));
        var context = new UrlResolutionContext(Array.Empty<IResolutionFeature>());

        var result = condition.IsMatch(context);

        Assert.True(result);
    }
}