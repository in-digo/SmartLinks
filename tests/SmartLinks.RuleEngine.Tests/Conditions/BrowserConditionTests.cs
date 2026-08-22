using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Conditions;

public sealed class BrowserConditionTests
{
    // true, если браузер запроса совпадает с браузером условия
    [Fact]
    public void IsMatchReturnsTrueWhenBrowserMatches()
    {
        const string browser = "chrome";
        var browserFeature = new BrowserFeature(browser);
        var condition = new BrowserCondition(browser);
        var context = new UrlResolutionContext(new IResolutionFeature[] { browserFeature });

        var result = condition.IsMatch(context);

        Assert.True(result);
    }

    // true, если названия браузеров отличаются только регистром
    [Fact]
    public void IsMatchReturnsTrueWhenBrowsersDifferOnlyByCase()
    {
        var browserFeature = new BrowserFeature("Chrome");
        var condition = new BrowserCondition("chrome");
        var context = new UrlResolutionContext(new IResolutionFeature[] { browserFeature });

        var result = condition.IsMatch(context);

        Assert.True(result);
    }

    // false, если браузер запроса не совпадает с браузером условия
    [Fact]
    public void IsMatchReturnsFalseWhenBrowserDoesNotMatch()
    {
        var browserFeature = new BrowserFeature("firefox");
        var condition = new BrowserCondition("chrome");
        var context = new UrlResolutionContext(new IResolutionFeature[] { browserFeature });

        var result = condition.IsMatch(context);

        Assert.False(result);
    }

    // true, если условие настроено для неизвестного браузера
    [Fact]
    public void IsMatchReturnsTrueWhenUnknownBrowserMatches()
    {
        var browserFeature = new BrowserFeature("unknown");
        var condition = new BrowserCondition("unknown");
        var context = new UrlResolutionContext(new IResolutionFeature[] { browserFeature });

        var result = condition.IsMatch(context);

        Assert.True(result);
    }

    // false, если браузер определить не удалось
    [Fact]
    public void IsMatchReturnsFalseWhenBrowserIsMissing()
    {
        var browserFeature = new BrowserFeature(null);
        var condition = new BrowserCondition("unknown");
        var context = new UrlResolutionContext(new IResolutionFeature[] { browserFeature });

        var result = condition.IsMatch(context);

        Assert.False(result);
    }
}