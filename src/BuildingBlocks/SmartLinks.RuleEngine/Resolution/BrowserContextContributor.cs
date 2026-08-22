namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Добавляет в контекст браузер клиента
/// </summary>
public sealed class BrowserContextContributor : IResolutionContextContributor
{
    private readonly IClientBrowserResolver _clientBrowserResolver;

    /// <summary>
    /// Создаёт contributor с определителем браузера
    /// </summary>
    public BrowserContextContributor(IClientBrowserResolver clientBrowserResolver)
    {
        _clientBrowserResolver = clientBrowserResolver;
    }

    /// <summary>
    /// Определяет браузер и добавляет его в контекст
    /// </summary>
    public void Contribute(UrlResolutionContextBuilder builder, UrlResolutionRequest request)
    {
        var browser = _clientBrowserResolver.ResolveBrowser(request.UserAgent);

        builder.Add(new BrowserFeature(browser));
    }
}