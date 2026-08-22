using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Conditions;

/// <summary>
/// Проверяет соответствие браузера клиента
/// </summary>
public sealed class BrowserCondition : ICompiledCondition
{
    private readonly string _browser;

    /// <summary>
    /// Создаёт условие для заданного браузера
    /// </summary>
    public BrowserCondition(string browser)
    {
        _browser = browser;
    }

    /// <summary>
    /// Проверяет соответствие браузера клиента
    /// </summary>
    public bool IsMatch(UrlResolutionContext context)
    {
        var browserFeature = context.GetRequiredFeature<BrowserFeature>();

        return string.Equals(browserFeature.Browser, _browser, StringComparison.OrdinalIgnoreCase);
    }
}