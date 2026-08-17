namespace SmartLinks.RuleEngine.Resolution;

// Определяет контракт выбора целевого URL
public interface ISmartLinkResolver
{
    // Разрешает целевой URL для переданной умной ссылки
    UrlResolutionResult Resolve(SmartLinkConfiguration smartLink, UrlResolutionContext context);
}