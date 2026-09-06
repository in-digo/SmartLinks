using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Resolution;

public sealed class UrlResolutionContextTests
{
    // Сохраняет все переданные типизированные признаки
    [Fact]
    public void ConstructorStoresAllFeatures()
    {
        var firstFeature = new StubFeature("value");
        var secondFeature = new AnotherStubFeature(42);
        var features = new IResolutionFeature[] { firstFeature, secondFeature };

        var context = new UrlResolutionContext(features);

        Assert.Same(firstFeature, context.GetRequiredFeature<StubFeature>());
        Assert.Same(secondFeature, context.GetRequiredFeature<AnotherStubFeature>());
    }

    // Возвращает признак запрошенного типа
    [Fact]
    public void GetRequiredFeatureReturnsFeatureOfRequestedType()
    {
        var feature = new StubFeature("value");
        var context = new UrlResolutionContext(
            new IResolutionFeature[] { feature });

        var result = context.GetRequiredFeature<StubFeature>();

        Assert.Same(feature, result);
    }

    private sealed record AnotherStubFeature(int Value) : IResolutionFeature;
    private sealed record StubFeature(string Value) : IResolutionFeature;
}