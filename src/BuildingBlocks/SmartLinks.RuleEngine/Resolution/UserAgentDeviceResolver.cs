namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Определяет тип устройства по User-Agent
/// </summary>
public sealed class UserAgentDeviceResolver : IClientDeviceResolver
{
    /// <summary>
    /// Возвращает тип устройства, определённый по User-Agent
    /// </summary>
    public string? ResolveDeviceType(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return null;

        if (IsAutomatedClient(userAgent))
            return "unknown";

        var isMobile = Contains(userAgent, "Mobile");
        var isIpad = Contains(userAgent, "iPad");
        var isAndroidTablet = Contains(userAgent, "Android") && !isMobile;

        if (isIpad || isAndroidTablet)
            return "tablet";

        if (isMobile)
            return "mobile";

        if (Contains(userAgent, "Windows") || Contains(userAgent, "Macintosh") || Contains(userAgent, "X11"))
            return "desktop";

        return "unknown";
    }

    /// <summary>
    /// Проверяет принадлежность User-Agent автоматизированному клиенту
    /// </summary>
    private static bool IsAutomatedClient(string userAgent)
    {
        return Contains(userAgent, "bot") || Contains(userAgent, "crawler") || Contains(userAgent, "spider");
    }

    /// <summary>
    /// Проверяет наличие маркера в User-Agent без учёта регистра
    /// </summary>
    private static bool Contains(string userAgent, string marker)
    {
        return userAgent.Contains(marker, StringComparison.OrdinalIgnoreCase);
    }
}