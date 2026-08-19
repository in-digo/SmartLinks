namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Создаёт контекст разрешения URL с использованием внедрённого источника времени
/// </summary>
public sealed class UrlResolutionContextFactory
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Создаёт фабрику с заданным источником времени
    /// </summary>
    public UrlResolutionContextFactory(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Создаёт контекст с текущим UTC-временем
    /// </summary>
    public UrlResolutionContext Create()
    {
        return new UrlResolutionContext(_timeProvider.GetUtcNow());
    }
}