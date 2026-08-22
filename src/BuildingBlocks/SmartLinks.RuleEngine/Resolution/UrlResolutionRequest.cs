using System.Net;

namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Содержит исходные данные запроса, используемые при формировании признаков
/// </summary>
public sealed record UrlResolutionRequest(IPAddress? IpAddress = null, string? UserAgent = null);