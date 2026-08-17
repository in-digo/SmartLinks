using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Resolution;

public sealed class SmartLinkResolverTests
{
    // Возвращает URL по умолчанию, если у активной умной ссылки нет правил
    [Fact]
    public void ResolveReturnsDefaultUrlWhenRulesAreEmpty()
    {
        const string defaultUrl = "https://example.com/default";

        var smartLink = new SmartLinkConfiguration(
            IsActive: true,
            DefaultUrl: defaultUrl,
            Rules: Array.Empty<SmartLinkRule>());

        var context = new UrlResolutionContext(UtcNow: new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero));

        var resolver = new SmartLinkResolver();

        var result = resolver.Resolve(smartLink, context);

        Assert.Equal(UrlResolutionStatus.Resolved, result.Status);
        Assert.Equal(defaultUrl, result.TargetUrl);
    }
}