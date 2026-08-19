using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Resolution;

public sealed class UrlResolutionContextFactoryTests
{
    // Использует текущее UTC-время из внедрённого TimeProvider
    [Fact]
    public void CreateUsesUtcNowFromTimeProvider()
    {
        var utcNow = new DateTimeOffset(2026, 8, 19, 18, 0, 0, TimeSpan.Zero);
        var timeProvider = new StubTimeProvider(utcNow);
        var factory = new UrlResolutionContextFactory(timeProvider);

        var context = factory.Create();

        Assert.Equal(utcNow, context.UtcNow);
    }

    private sealed class StubTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        // Создаёт провайдер с заданным UTC-временем
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