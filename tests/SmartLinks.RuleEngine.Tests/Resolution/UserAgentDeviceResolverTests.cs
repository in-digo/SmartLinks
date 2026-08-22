using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Resolution;

public sealed class UserAgentDeviceResolverTests
{
    // Определяет мобильное устройство по User-Agent iPhone
    [Fact]
    public void ResolveDeviceTypeReturnsMobileForIPhoneUserAgent()
    {
        const string userAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_5) AppleWebKit Mobile Safari";
        var resolver = new UserAgentDeviceResolver();

        var result = resolver.ResolveDeviceType(userAgent);

        Assert.Equal("mobile", result);
    }

    // Определяет планшет по User-Agent iPad
    [Fact]
    public void ResolveDeviceTypeReturnsTabletForIPadUserAgent()
    {
        const string userAgent = "Mozilla/5.0 (iPad; CPU OS 17_5 like Mac OS X) AppleWebKit Mobile Safari";
        var resolver = new UserAgentDeviceResolver();

        var result = resolver.ResolveDeviceType(userAgent);

        Assert.Equal("tablet", result);
    }

    // Определяет Android-планшет при отсутствии маркера Mobile
    [Fact]
    public void ResolveDeviceTypeReturnsTabletForAndroidUserAgentWithoutMobileMarker()
    {
        const string userAgent = "Mozilla/5.0 (Linux; Android 14; SM-X710) AppleWebKit Chrome Safari";
        var resolver = new UserAgentDeviceResolver();

        var result = resolver.ResolveDeviceType(userAgent);

        Assert.Equal("tablet", result);
    }

    // Определяет desktop по User-Agent Windows
    [Fact]
    public void ResolveDeviceTypeReturnsDesktopForWindowsUserAgent()
    {
        const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit Chrome Safari";
        var resolver = new UserAgentDeviceResolver();

        var result = resolver.ResolveDeviceType(userAgent);

        Assert.Equal("desktop", result);
    }

    // Определяет мобильное устройство по User-Agent Android-телефона
    [Fact]
    public void ResolveDeviceTypeReturnsMobileForAndroidPhoneUserAgent()
    {
        const string userAgent = "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit Chrome Mobile Safari";
        var resolver = new UserAgentDeviceResolver();

        var result = resolver.ResolveDeviceType(userAgent);

        Assert.Equal("mobile", result);
    }

    // Определяет desktop по User-Agent macOS
    [Fact]
    public void ResolveDeviceTypeReturnsDesktopForMacintoshUserAgent()
    {
        const string userAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_5) AppleWebKit Version Safari";
        var resolver = new UserAgentDeviceResolver();

        var result = resolver.ResolveDeviceType(userAgent);

        Assert.Equal("desktop", result);
    }

    // Определяет desktop по User-Agent Linux
    [Fact]
    public void ResolveDeviceTypeReturnsDesktopForLinuxUserAgent()
    {
        const string userAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit Chrome Safari";
        var resolver = new UserAgentDeviceResolver();

        var result = resolver.ResolveDeviceType(userAgent);

        Assert.Equal("desktop", result);
    }

    // Возвращает unknown для автоматизированного клиента
    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0) Googlebot/2.1")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0) ExampleCrawler/1.0")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0) ExampleSpider/1.0")]
    public void ResolveDeviceTypeReturnsUnknownForAutomatedUserAgent(string userAgent)
    {
        var resolver = new UserAgentDeviceResolver();

        var result = resolver.ResolveDeviceType(userAgent);

        Assert.Equal("unknown", result);
    }

    // Возвращает unknown для нераспознанного непустого User-Agent
    [Fact]
    public void ResolveDeviceTypeReturnsUnknownForUnrecognizedUserAgent()
    {
        const string userAgent = "CustomClient/1.0";
        var resolver = new UserAgentDeviceResolver();

        var result = resolver.ResolveDeviceType(userAgent);

        Assert.Equal("unknown", result);
    }

    // Возвращает null для отсутствующего или пустого User-Agent
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveDeviceTypeReturnsNullWhenUserAgentIsMissing(string? userAgent)
    {
        var resolver = new UserAgentDeviceResolver();

        var result = resolver.ResolveDeviceType(userAgent);

        Assert.Null(result);
    }
}