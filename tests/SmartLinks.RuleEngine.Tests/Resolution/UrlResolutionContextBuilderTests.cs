using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Resolution;

public sealed class UrlResolutionContextBuilderTests
{
    // Создаёт контекст, содержащий добавленный типизированный признак
    [Fact]
    public void BuildReturnsContextContainingAddedFeature()
    {
        var feature = new StubFeature("value");
        var builder = new UrlResolutionContextBuilder();

        builder.Add(feature);

        var context = builder.Build();

        Assert.Same(feature, context.GetRequiredFeature<StubFeature>());
    }

    private sealed record StubFeature(string Value) : IResolutionFeature;
}