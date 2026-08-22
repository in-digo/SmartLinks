namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Формирует контекст разрешения URL из типизированных признаков
/// </summary>
public sealed class UrlResolutionContextBuilder
{
    private readonly Dictionary<Type, IResolutionFeature> _features = new();

    /// <summary>
    /// Добавляет типизированный признак
    /// </summary>
    public void Add(IResolutionFeature feature)
    {
        _features.Add(feature.GetType(), feature);
    }

    /// <summary>
    /// Создаёт контекст из добавленных признаков
    /// </summary>
    public UrlResolutionContext Build()
    {
        return new UrlResolutionContext(_features.Values);
    }
}