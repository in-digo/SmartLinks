namespace SmartLinks.RuleEngine.Resolution;

// Выбирает целевой URL по конфигурации умной ссылки и контексту запроса
public sealed class SmartLinkResolver : ISmartLinkResolver
{
    // Разрешает целевой URL для переданной умной ссылки
    public UrlResolutionResult Resolve(SmartLinkConfiguration smartLink, UrlResolutionContext context)
    {
        if (!smartLink.IsActive)
            return new UrlResolutionResult(UrlResolutionStatus.SmartLinkInactive, null);

        var matchingRule = smartLink.Rules
            .Where(rule => rule.IsEnabled)
            .OrderBy(rule => rule.Priority)
            .FirstOrDefault(rule => rule.Condition.IsMatch(context));

        var targetUrl = matchingRule?.TargetUrl ?? smartLink.DefaultUrl;

        return new UrlResolutionResult(UrlResolutionStatus.Resolved, targetUrl);
    }
}