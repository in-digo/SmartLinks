using System.Net;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Resolution;

public sealed class UrlResolutionContextTests
{
    // Сохраняет все переданные характеристики контекста разрешения URL
    [Fact]
    public void ConstructorStoresAllContextValues()
    {
        var utcNow = new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);
        var ipAddress = IPAddress.Parse("203.0.113.10");
        const string countryCode = "RU";
        const string deviceType = "Mobile";
        const string browser = "Chrome";
        const string userAgent = "SmartLinks-Test-Agent";

        var context = new UrlResolutionContext(utcNow, ipAddress, countryCode, deviceType, browser, userAgent);

        Assert.Equal(utcNow, context.UtcNow);
        Assert.Equal(ipAddress, context.IpAddress);
        Assert.Equal(countryCode, context.CountryCode);
        Assert.Equal(deviceType, context.DeviceType);
        Assert.Equal(browser, context.Browser);
        Assert.Equal(userAgent, context.UserAgent);
    }
}