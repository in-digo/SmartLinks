using System.Text.Json;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Conditions;

/// <summary>
/// Создаёт условия проверки страны
/// </summary>
public sealed class CountryConditionFactory : IConditionFactory
{
    /// <summary>
    /// Тип создаваемого условия
    /// </summary>
    public string Type => "country";

    /// <summary>
    /// Создаёт условие из JSON-параметров
    /// </summary>
    public ICompiledCondition Create(JsonElement parameters)
    {
        var countryCode = parameters.GetProperty("countryCode").GetString()!;

        return new CountryCondition(countryCode);
    }
}