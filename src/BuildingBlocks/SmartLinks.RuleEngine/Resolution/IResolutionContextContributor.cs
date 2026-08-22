namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Добавляет типизированные признаки в контекст разрешения URL
/// </summary>
public interface IResolutionContextContributor
{
    /// <summary>
    /// Добавляет признаки, полученные из исходных данных запроса
    /// </summary>
    void Contribute(
        UrlResolutionContextBuilder builder,
        UrlResolutionRequest request);
}