namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Добавляет в контекст текущее UTC-время
/// </summary>
public sealed class UtcTimeContextContributor : IResolutionContextContributor
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Создаёт contributor с заданным источником времени
    /// </summary>
    public UtcTimeContextContributor(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Добавляет текущее UTC-время в контекст
    /// </summary>
    public void Contribute(UrlResolutionContextBuilder builder, UrlResolutionRequest request)
    {
        builder.Add(new UtcTimeFeature(_timeProvider.GetUtcNow()));
    }
}