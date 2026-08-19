using System.Text.Json;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Conditions;

/// <summary>
/// Создаёт условия проверки UTC-интервала времени
/// </summary>
public sealed class UtcDateTimeRangeConditionFactory : IConditionFactory
{
    /// <summary>
    /// Тип создаваемого условия
    /// </summary>
    public string Type => "utc-date-time-range";

    /// <summary>
    /// Создаёт условие из JSON-параметров
    /// </summary>
    public ICompiledCondition Create(JsonElement parameters)
    {
        var fromUtc = parameters.GetProperty("fromUtc").GetDateTimeOffset();
        var toUtc = parameters.GetProperty("toUtc").GetDateTimeOffset();

        return new UtcDateTimeRangeCondition(fromUtc, toUtc);
    }
}