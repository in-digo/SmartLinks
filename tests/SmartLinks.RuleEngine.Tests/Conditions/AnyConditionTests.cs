using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Conditions;

public class AnyConditionTests
{
    /// <summary>
    /// Проверяет отсутствие совпадения, если ни одно вложенное условие не выполняется
    /// </summary>
    [Fact]
    public void IsMatchReturnsFalseWhenNoConditionsMatch()
    {
        var condition = new AnyCondition([new StubCondition(false), new StubCondition(false)]);
        var context = new UrlResolutionContext(Array.Empty<IResolutionFeature>());

        var result = condition.IsMatch(context);

        Assert.False(result);
    }

    /// <summary>
    /// Проверяет совпадение, если хотя бы одно вложенное условие выполняется
    /// </summary>
    [Fact]
    public void IsMatchReturnsTrueWhenAnyConditionMatches()
    {
        var condition = new AnyCondition([new StubCondition(false), new StubCondition(true)]);
        var context = new UrlResolutionContext(Array.Empty<IResolutionFeature>());

        var result = condition.IsMatch(context);

        Assert.True(result);
    }
}