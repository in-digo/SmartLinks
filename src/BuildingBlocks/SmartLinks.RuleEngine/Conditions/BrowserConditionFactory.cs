using System.Text.Json;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Conditions;

/// <summary>
/// Создаёт условия проверки браузера
/// </summary>
public sealed class BrowserConditionFactory : IConditionFactory
{
    /// <summary>
    /// Тип создаваемого условия
    /// </summary>
    public string Type => "browser";

    /// <summary>
    /// Создаёт условие из JSON-параметров
    /// </summary>
    public ICompiledCondition Create(JsonElement parameters)
    {
        var browser = parameters.GetProperty("browser").GetString()!;

        return new BrowserCondition(browser);
    }
}