using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Conditions;

public class AllConditionTests
{
    /// <summary>
    /// Проверяет совпадение при выполнении всех вложенных условий
    /// </summary>
    [Fact]
    public void IsMatchReturnsTrueWhenAllConditionsMatch()
    {
        var condition = new AllCondition([new StubCondition(true), new StubCondition(true)]);
        var context = new UrlResolutionContext(Array.Empty<IResolutionFeature>());

        var result = condition.IsMatch(context);

        Assert.True(result);
    }

    /// <summary>
    /// Проверяет отсутствие совпадения, если хотя бы одно вложенное условие не выполняется
    /// </summary>
    [Fact]
    public void IsMatchReturnsFalseWhenAnyConditionDoesNotMatch()
    {
        var condition = new AllCondition([new StubCondition(true), new StubCondition(false)]);
        var context = new UrlResolutionContext(Array.Empty<IResolutionFeature>());

        var result = condition.IsMatch(context);

        Assert.False(result);
    }
}