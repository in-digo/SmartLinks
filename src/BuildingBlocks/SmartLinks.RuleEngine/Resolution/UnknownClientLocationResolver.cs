using System.Net;

namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Возвращает неизвестную страну, если внешний определитель местоположения не настроен
/// </summary>
public sealed class UnknownClientLocationResolver : IClientLocationResolver
{
    /// <summary>
    /// Возвращает неизвестный код страны
    /// </summary>
    public string? ResolveCountryCode(IPAddress? ipAddress)
    {
        return null;
    }
}