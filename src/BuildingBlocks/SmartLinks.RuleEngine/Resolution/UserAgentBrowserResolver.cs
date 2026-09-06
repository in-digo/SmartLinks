namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Определяет браузер по User-Agent
/// </summary>
public sealed class UserAgentBrowserResolver : IClientBrowserResolver
{
    /// <summary>
    /// Возвращает браузер, определённый по User-Agent
    /// </summary>
    public string? ResolveBrowser(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return null;

        if (IsAutomatedClient(userAgent))
            return "unknown";

        // Edge и Opera проверяются раньше Chrome, поскольку их User-Agent содержит Chrome
        if (Contains(userAgent, "Edg/") || Contains(userAgent, "EdgiOS/") || Contains(userAgent, "EdgA/"))
            return "edge";

        if (Contains(userAgent, "OPR/") || Contains(userAgent, "Opera"))
            return "opera";

        if (Contains(userAgent, "Chrome/") || Contains(userAgent, "CriOS/"))
            return "chrome";

        // Safari проверяется после Chrome и мобильных вариантов других браузеров
        if (Contains(userAgent, "Firefox/") || Contains(userAgent, "FxiOS/"))
            return "firefox";

        if (Contains(userAgent, "Safari/") && Contains(userAgent, "Version/"))
            return "safari";

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