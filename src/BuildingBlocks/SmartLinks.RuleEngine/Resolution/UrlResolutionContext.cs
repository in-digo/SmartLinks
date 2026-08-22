namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Содержит типизированные признаки, используемые при проверке правил умной ссылки
/// </summary>
public sealed class UrlResolutionContext
{
    private readonly IReadOnlyDictionary<Type, IResolutionFeature> _features;

    /// <summary>
    /// Создаёт контекст из набора типизированных признаков
    /// </summary>
    public UrlResolutionContext(IEnumerable<IResolutionFeature> features)
    {
        _features = features.ToDictionary(feature => feature.GetType());
    }

    /// <summary>
    /// Возвращает обязательный признак заданного типа
    /// </summary>
    public TFeature GetRequiredFeature<TFeature>() where TFeature : IResolutionFeature
    {
        return (TFeature)_features[typeof(TFeature)];
    }
}