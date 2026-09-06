using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Conditions;

/// <summary>
/// Проверяет соответствие типа устройства клиента
/// </summary>
public sealed class DeviceCondition : ICompiledCondition
{
    private readonly string _deviceType;

    /// <summary>
    /// Создаёт условие для заданного типа устройства
    /// </summary>
    public DeviceCondition(string deviceType)
    {
        _deviceType = deviceType;
    }

    /// <summary>
    /// Проверяет соответствие типа устройства клиента
    /// </summary>
    public bool IsMatch(UrlResolutionContext context)
    {
        var deviceFeature = context.GetRequiredFeature<DeviceFeature>();

        return string.Equals(deviceFeature.DeviceType, _deviceType, StringComparison.OrdinalIgnoreCase);    
    }
}