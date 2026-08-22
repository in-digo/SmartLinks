using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Conditions;

public sealed class CountryConditionTests
{
    // true, если код страны запроса совпадает с кодом страны условия
    [Fact]
    public void IsMatchReturnsTrueWhenCountryCodeMatches()
    {
        const string countryCode = "RU";

        var condition = new CountryCondition(countryCode);
        var context = new UrlResolutionContext(
            new IResolutionFeature[] { new CountryFeature(countryCode) });

        var result = condition.IsMatch(context);

        Assert.True(result);
    }

    // true, если коды стран отличаются только регистром
    [Fact]
    public void IsMatchReturnsTrueWhenCountryCodesDifferOnlyByCase()
    {
        var countryFeature = new CountryFeature("ru");
        var condition = new CountryCondition("RU");
        var context = new UrlResolutionContext(new IResolutionFeature[] { countryFeature });

        var result = condition.IsMatch(context);

        Assert.True(result);
    }
}