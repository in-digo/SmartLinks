using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Resolution;

public sealed class UserAgentBrowserResolverTests
{
    // Определяет Microsoft Edge раньше вложенного маркера Chrome
    [Theory]
    [InlineData("Mozilla/5.0 Chrome/126 Safari/537.36 Edg/126")]
    [InlineData("Mozilla/5.0 AppleWebKit EdgiOS/126 Mobile Safari")]
    [InlineData("Mozilla/5.0 Android Chrome/126 Mobile Safari EdgA/126")]
    public void ResolveBrowserReturnsEdgeForEdgeUserAgent(string userAgent)
    {
        var resolver = new UserAgentBrowserResolver();

        var result = resolver.ResolveBrowser(userAgent);

        Assert.Equal("edge", result);
    }

    // Определяет Opera раньше вложенного маркера Chrome
    [Theory]
    [InlineData("Mozilla/5.0 Chrome/126 Safari/537.36 OPR/111")]
    [InlineData("Opera/9.80 Presto/2.12")]
    public void ResolveBrowserReturnsOperaForOperaUserAgent(string userAgent)
    {
        var resolver = new UserAgentBrowserResolver();

        var result = resolver.ResolveBrowser(userAgent);

        Assert.Equal("opera", result);
    }

    // Определяет Chrome по настольному или мобильному User-Agent
    [Theory]
    [InlineData("Mozilla/5.0 Chrome/126 Safari/537.36")]
    [InlineData("Mozilla/5.0 CriOS/126 Mobile Safari/604.1")]
    public void ResolveBrowserReturnsChromeForChromeUserAgent(string userAgent)
    {
        var resolver = new UserAgentBrowserResolver();

        var result = resolver.ResolveBrowser(userAgent);

        Assert.Equal("chrome", result);
    }

    // Определяет Firefox по настольному или мобильному User-Agent
    [Theory]
    [InlineData("Mozilla/5.0 Firefox/128")]
    [InlineData("Mozilla/5.0 FxiOS/128 Mobile Safari")]
    public void ResolveBrowserReturnsFirefoxForFirefoxUserAgent(string userAgent)
    {
        var resolver = new UserAgentBrowserResolver();

        var result = resolver.ResolveBrowser(userAgent);

        Assert.Equal("firefox", result);
    }

    // Определяет Safari по сочетанию Version и Safari
    [Fact]
    public void ResolveBrowserReturnsSafariForSafariUserAgent()
    {
        const string userAgent = "Mozilla/5.0 AppleWebKit Version/17.5 Safari/605.1.15";
        var resolver = new UserAgentBrowserResolver();

        var result = resolver.ResolveBrowser(userAgent);

        Assert.Equal("safari", result);
    }

    // Возвращает unknown для автоматизированного клиента
    [Theory]
    [InlineData("Mozilla/5.0 Chrome/126 Safari/537.36 Googlebot/2.1")]
    [InlineData("Mozilla/5.0 Chrome/126 Safari/537.36 ExampleCrawler/1.0")]
    [InlineData("Mozilla/5.0 Chrome/126 Safari/537.36 ExampleSpider/1.0")]
    public void ResolveBrowserReturnsUnknownForAutomatedUserAgent(string userAgent)
    {
        var resolver = new UserAgentBrowserResolver();

        var result = resolver.ResolveBrowser(userAgent);

        Assert.Equal("unknown", result);
    }

    // Возвращает unknown для нераспознанного непустого User-Agent
    [Fact]
    public void ResolveBrowserReturnsUnknownForUnrecognizedUserAgent()
    {
        const string userAgent = "CustomClient/1.0";
        var resolver = new UserAgentBrowserResolver();

        var result = resolver.ResolveBrowser(userAgent);

        Assert.Equal("unknown", result);
    }

    // Возвращает null для отсутствующего или пустого User-Agent
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveBrowserReturnsNullWhenUserAgentIsMissing(string? userAgent)
    {
        var resolver = new UserAgentBrowserResolver();

        var result = resolver.ResolveBrowser(userAgent);

        Assert.Null(result);
    }
}