using System.Text.Json;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Conditions;

/// <summary>
/// Создаёт скомпилированные условия с помощью зарегистрированных фабрик
/// </summary>
public sealed class ConditionCompiler
{
    private readonly IEnumerable<IConditionFactory> _conditionFactories;

    /// <summary>
    /// Создаёт компилятор с доступными фабриками условий
    /// </summary>
    public ConditionCompiler(IEnumerable<IConditionFactory> conditionFactories)
    {
        _conditionFactories = conditionFactories;
    }

    /// <summary>
    /// Создаёт условие с помощью фабрики соответствующего типа
    /// </summary>
    public ICompiledCondition Compile(string type, JsonElement parameters)
    {
        var conditionFactory = _conditionFactories.FirstOrDefault(factory => factory.Type == type);
        if (conditionFactory is null)
            throw new InvalidOperationException($"Фабрика условия типа '{type}' не зарегистрирована");

        return conditionFactory.Create(parameters);
    }
}