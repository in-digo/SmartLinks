using System.Text.Json;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Conditions;

/// <summary>
/// Создаёт условия проверки типа устройства
/// </summary>
public sealed class DeviceConditionFactory : IConditionFactory
{
    /// <summary>
    /// Тип создаваемого условия
    /// </summary>
    public string Type => "device";

    /// <summary>
    /// Создаёт условие из JSON-параметров
    /// </summary>
    public ICompiledCondition Create(JsonElement parameters)
    {
        var deviceType = parameters.GetProperty("deviceType").GetString()!;

        return new DeviceCondition(deviceType);
    }
}