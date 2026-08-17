using System.Net;

namespace SmartLinks.RuleEngine.Resolution;

// Содержит данные запроса, используемые при проверке правил умной ссылки
public sealed record UrlResolutionContext(
    DateTimeOffset UtcNow,
    IPAddress? IpAddress = null,
    string? CountryCode = null,
    string? DeviceType = null,
    string? Browser = null,
    string? UserAgent = null);