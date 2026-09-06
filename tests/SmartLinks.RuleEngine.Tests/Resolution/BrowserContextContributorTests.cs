using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Resolution;

public sealed class BrowserContextContributorTests
{
    // Добавляет браузер, определённый по User-Agent запроса
    [Fact]
    public void ContributeAddsBrowserFeatureResolvedFromUserAgent()
    {
        const string userAgent = "test-user-agent";
        const string browser = "chrome";
        var browserResolver = new StubClientBrowserResolver(userAgent, browser);
        var contributor = new BrowserContextContributor(browserResolver);
        var builder = new UrlResolutionContextBuilder();

        contributor.Contribute(builder, new UrlResolutionRequest(UserAgent: userAgent));

        var context = builder.Build();
        var feature = context.GetRequiredFeature<BrowserFeature>();

        Assert.Equal(browser, feature.Browser);
    }

    private sealed class StubClientBrowserResolver : IClientBrowserResolver
    {
        private readonly string _expectedUserAgent;
        private readonly string _browser;

        // Создаёт определитель с ожидаемым User-Agent и браузером
        public StubClientBrowserResolver(string expectedUserAgent, string browser)
        {
            _expectedUserAgent = expectedUserAgent;
            _browser = browser;
        }

        // Возвращает браузер для ожидаемого User-Agent
        public string? ResolveBrowser(string? userAgent)
        {
            return userAgent == _expectedUserAgent ? _browser : null;
        }
    }
}