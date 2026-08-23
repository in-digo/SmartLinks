using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Conditions;

internal sealed class StubCondition : ICompiledCondition
{
    private readonly bool _isMatch;

    /// <summary>
    /// Создаёт условие с заданным результатом
    /// </summary>
    public StubCondition(bool isMatch)
    {
        _isMatch = isMatch;
    }

    /// <summary>
    /// Возвращает заданный результат проверки
    /// </summary>
    public bool IsMatch(UrlResolutionContext context)
    {
        return _isMatch;
    }
}