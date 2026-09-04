using System.Net;
using MaxMind.GeoIP2;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.Redirect.Infrastructure.GeoIp;

/// <summary>
/// Определяет страну клиента по локальной базе MaxMind
/// </summary>
public sealed class MaxMindClientLocationResolver : IClientLocationResolver, IDisposable
{
    private readonly DatabaseReader _databaseReader;

    /// <summary>
    /// Создаёт определитель страны с локальной базой MaxMind
    /// </summary>
    public MaxMindClientLocationResolver(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        _databaseReader = new DatabaseReader(databasePath);
    }

    /// <summary>
    /// Возвращает ISO-код страны или null, если IP-адрес отсутствует в базе
    /// </summary>
    public string? ResolveCountryCode(IPAddress? ipAddress)
    {
        if (ipAddress is null)
            return null;

        var normalizedIpAddress = ipAddress.IsIPv4MappedToIPv6 ? ipAddress.MapToIPv4() : ipAddress;
        if (!_databaseReader.TryCountry(normalizedIpAddress, out var response))
            return null;

        return response?.Country?.IsoCode;
    }

    /// <summary>
    /// Освобождает ресурсы чтения локальной базы MaxMind
    /// </summary>
    public void Dispose()
    {
        _databaseReader.Dispose();
    }
}