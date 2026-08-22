using System.Net;

namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Определяет код страны клиента по IP-адресу
/// </summary>
public interface IClientLocationResolver
{
    /// <summary>
    /// Возвращает код страны или null, если страну определить не удалось
    /// </summary>
    string? ResolveCountryCode(IPAddress? ipAddress);
}