using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Conditions;

public sealed class AnyCondition : ICompiledCondition
{
    private readonly ICompiledCondition[] _conditions;

    /// <summary>
    /// Создаёт составное условие из вложенных условий
    /// </summary>
    public AnyCondition(IEnumerable<ICompiledCondition> conditions)
    {
        _conditions = conditions.ToArray();
    }

    /// <summary>
    /// Проверяет выполнение хотя бы одного вложенного условия
    /// </summary>
    public bool IsMatch(UrlResolutionContext context)
    {
        return _conditions.Any(condition => condition.IsMatch(context));
    }
}