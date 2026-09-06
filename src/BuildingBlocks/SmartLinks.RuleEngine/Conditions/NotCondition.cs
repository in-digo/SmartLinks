using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Conditions;

public sealed class NotCondition : ICompiledCondition
{
    private readonly ICompiledCondition _condition;

    /// <summary>
    /// Создаёт отрицание вложенного условия
    /// </summary>
    public NotCondition(ICompiledCondition condition)
    {
        _condition = condition;
    }

    /// <summary>
    /// Инвертирует результат вложенного условия
    /// </summary>
    public bool IsMatch(UrlResolutionContext context)
    {
        return !_condition.IsMatch(context);
    }
}