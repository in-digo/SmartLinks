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

    // Выбирает совпавшее правило с наименьшим числовым значением приоритета
    [Fact]
    public void ResolveReturnsMatchingRuleWithLowestPriorityValue()
    {
        const string defaultUrl = "https://example.com/default";
        const string priorityTenUrl = "https://example.com/priority-10";
        const string priorityTwentyUrl = "https://example.com/priority-20";

        var priorityTwentyRule = new SmartLinkRule(20, true, priorityTwentyUrl, new StubCondition(true));
        var priorityTenRule = new SmartLinkRule(10, true, priorityTenUrl, new StubCondition(true));

        var smartLink = new SmartLinkConfiguration(
            true,
            defaultUrl,
            new[] { priorityTwentyRule, priorityTenRule });

        var context = new UrlResolutionContext(new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero));
        var resolver = new SmartLinkResolver();

        var result = resolver.Resolve(smartLink, context);

        Assert.Equal(UrlResolutionStatus.Resolved, result.Status);
        Assert.Equal(priorityTenUrl, result.TargetUrl);
    }

    // Игнорирует выключенное правило
    [Fact]
    public void ResolveIgnoresDisabledMatchingRule()
    {
        const string defaultUrl = "https://example.com/default";
        const string disabledRuleUrl = "https://example.com/disabled";
        const string enabledRuleUrl = "https://example.com/enabled";

        var disabledRule = new SmartLinkRule(1, false, disabledRuleUrl, new StubCondition(true));
        var enabledRule = new SmartLinkRule(2, true, enabledRuleUrl, new StubCondition(true));

        var smartLink = new SmartLinkConfiguration(true, defaultUrl, new[] { enabledRule, disabledRule });
        var context = new UrlResolutionContext(new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero));

        var resolver = new SmartLinkResolver();

        var result = resolver.Resolve(smartLink, context);

        Assert.Equal(UrlResolutionStatus.Resolved, result.Status);
        Assert.Equal(enabledRuleUrl, result.TargetUrl);
    }

    // Возвращает неактивный результат для выключенной умной ссылки
    [Fact]
    public void ResolveReturnsInactiveResultForInactiveSmartLink()
    {
        const string defaultUrl = "https://example.com/default";

        var rule = new SmartLinkRule(1, true, "https://example.com/matched", new StubCondition(true));
        var smartLink = new SmartLinkConfiguration(false, defaultUrl, new[] { rule });
        var context = new UrlResolutionContext(new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero));

        var resolver = new SmartLinkResolver();

        var result = resolver.Resolve(smartLink, context);

        Assert.Equal(UrlResolutionStatus.SmartLinkInactive, result.Status);
        Assert.Null(result.TargetUrl);
    }

    // Пропускает несовпавшее правило и выбирает следующее совпавшее по приоритету
    [Fact]
    public void ResolveSkipsNonMatchingRuleAndReturnsNextMatchingRule()
    {
        const string defaultUrl = "https://example.com/default";
        const string expectedUrl = "https://example.com/priority-20";

        var priorityTenRule = new SmartLinkRule(10, true, "https://example.com/priority-10", new StubCondition(false));
        var priorityTwentyRule = new SmartLinkRule(20, true, expectedUrl, new StubCondition(true));

        var smartLink = new SmartLinkConfiguration(true, defaultUrl, new[] { priorityTwentyRule, priorityTenRule });
        var context = new UrlResolutionContext(new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero));

        var resolver = new SmartLinkResolver();

        var result = resolver.Resolve(smartLink, context);

        Assert.Equal(UrlResolutionStatus.Resolved, result.Status);
        Assert.Equal(expectedUrl, result.TargetUrl);
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