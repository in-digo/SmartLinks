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

    // Возвращает целевой URL включённого правила, если его условие совпало
    [Fact]
    public void ResolveReturnsTargetUrlWhenEnabledRuleMatches()
    {
        const string defaultUrl = "https://example.com/default";
        const string targetUrl = "https://example.com/matched";

        var rule = new SmartLinkRule(1, true, targetUrl, new StubCondition(true));
        var smartLink = new SmartLinkConfiguration(true, defaultUrl, new[] { rule });
        var context = new UrlResolutionContext(new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero));

        var resolver = new SmartLinkResolver();

        var result = resolver.Resolve(smartLink, context);

        Assert.Equal(UrlResolutionStatus.Resolved, result.Status);
        Assert.Equal(targetUrl, result.TargetUrl);
    }

    // Возвращает URL по умолчанию, если ни одно правило не совпало
    [Fact]
    public void ResolveReturnsDefaultUrlWhenNoRuleMatches()
    {
        const string defaultUrl = "https://example.com/default";

        var rule = new SmartLinkRule(1, true, "https://example.com/not-matched", new StubCondition(false));

        var smartLink = new SmartLinkConfiguration(true, defaultUrl, new[] { rule });
        var context = new UrlResolutionContext(new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero));

        var resolver = new SmartLinkResolver();

        var result = resolver.Resolve(smartLink, context);

        Assert.Equal(UrlResolutionStatus.Resolved, result.Status);
        Assert.Equal(defaultUrl, result.TargetUrl);
    }

    private sealed class StubCondition : ICompiledCondition
    {
        private readonly bool _result;

        // Создаёт условие с заданным результатом проверки
        public StubCondition(bool result)
        {
            _result = result;
        }

        // Возвращает заданный результат проверки условия
        public bool IsMatch(UrlResolutionContext context)
        {
            return _result;
        }
    }
}