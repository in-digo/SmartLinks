namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Создаёт контекст разрешения URL с помощью зарегистрированных contributors
/// </summary>
public sealed class UrlResolutionContextFactory
{
    private readonly IReadOnlyCollection<IResolutionContextContributor> _contributors;

    /// <summary>
    /// Создаёт фабрику с зарегистрированными contributors
    /// </summary>
    public UrlResolutionContextFactory(IEnumerable<IResolutionContextContributor> contributors)
    {
        _contributors = contributors.ToArray();
    }

    /// <summary>
    /// Создаёт контекст с типизированными признаками запроса
    /// </summary>
    public UrlResolutionContext Create(UrlResolutionRequest request)
    {
        var builder = new UrlResolutionContextBuilder();

        foreach (var contributor in _contributors)
            contributor.Contribute(builder, request);

        return builder.Build();
    }
}