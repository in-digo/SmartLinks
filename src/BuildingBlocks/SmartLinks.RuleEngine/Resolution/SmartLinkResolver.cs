namespace SmartLinks.RuleEngine.Resolution;

// Выбирает целевой URL по конфигурации умной ссылки и контексту запроса
public sealed class SmartLinkResolver : ISmartLinkResolver
{
    // Разрешает целевой URL для переданной умной ссылки
    public UrlResolutionResult Resolve(SmartLinkConfiguration smartLink, UrlResolutionContext context)
    {
        var matchingRule = smartLink.Rules
            .OrderBy(rule => rule.Priority)
            .FirstOrDefault(rule => rule.Condition.IsMatch(context));

        var targetUrl = matchingRule?.TargetUrl ?? smartLink.DefaultUrl;

        return new UrlResolutionResult(UrlResolutionStatus.Resolved, targetUrl);
    }
}