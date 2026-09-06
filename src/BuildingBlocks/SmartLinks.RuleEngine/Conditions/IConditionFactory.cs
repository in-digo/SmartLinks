using System.Text.Json;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Conditions;

/// <summary>
/// Создаёт скомпилированные условия определённого типа
/// </summary>
public interface IConditionFactory
{
    /// <summary>
    /// Тип создаваемого условия
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Создаёт условие из переданных параметров
    /// </summary>
    ICompiledCondition Create(JsonElement parameters);
}