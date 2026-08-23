using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Conditions;

public sealed class AllCondition : ICompiledCondition
{
    private readonly ICompiledCondition[] _conditions;

    /// <summary>
    /// Создаёт составное условие из вложенных условий
    /// </summary>
    public AllCondition(IEnumerable<ICompiledCondition> conditions)
    {
        _conditions = conditions.ToArray();
    }

    /// <summary>
    /// Проверяет выполнение всех вложенных условий
    /// </summary>
    public bool IsMatch(UrlResolutionContext context)
    {
        return _conditions.All(condition => condition.IsMatch(context));
    }
}