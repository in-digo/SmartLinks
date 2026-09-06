using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Resolution;

public sealed class UtcTimeContextContributorTests
{
    // Добавляет признак текущего UTC-времени
    [Fact]
    public void ContributeAddsCurrentUtcTimeFeature()
    {
        var utcNow = new DateTimeOffset(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new StubTimeProvider(utcNow);
        var contributor = new UtcTimeContextContributor(timeProvider);
        var builder = new UrlResolutionContextBuilder();

        contributor.Contribute(builder, new UrlResolutionRequest());

        var context = builder.Build();
        var feature = context.GetRequiredFeature<UtcTimeFeature>();

        Assert.Equal(utcNow, feature.UtcNow);
    }

    private sealed class StubTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        // Создаёт источник с заданным UTC-временем
        public StubTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        // Возвращает заданное UTC-время
        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}