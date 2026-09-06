namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Определяет браузер клиента по User-Agent
/// </summary>
public interface IClientBrowserResolver
{
    /// <summary>
    /// Возвращает браузер или null, если User-Agent отсутствует
    /// </summary>
    string? ResolveBrowser(string? userAgent);
}