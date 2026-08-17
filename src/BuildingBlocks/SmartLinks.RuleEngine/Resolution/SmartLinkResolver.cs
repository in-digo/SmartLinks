namespace SmartLinks.RuleEngine.Resolution;

// Выбирает целевой URL по конфигурации умной ссылки и контексту запроса
public sealed class SmartLinkResolver : ISmartLinkResolver
{
    // Разрешает целевой URL для переданной умной ссылки
    public UrlResolutionResult Resolve(SmartLinkConfiguration smartLink, UrlResolutionContext context)
    {
        return new UrlResolutionResult(UrlResolutionStatus.Resolved, smartLink.DefaultUrl);
    }
}