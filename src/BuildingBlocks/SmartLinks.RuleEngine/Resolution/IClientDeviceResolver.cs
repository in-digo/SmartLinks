namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Определяет тип устройства клиента по User-Agent
/// </summary>
public interface IClientDeviceResolver
{
    /// <summary>
    /// Возвращает тип устройства или null, если определить его невозможно
    /// </summary>
    string? ResolveDeviceType(string? userAgent);
}