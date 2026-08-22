using SmartLinks.RuleEngine.Resolution;
using System.Net;

namespace SmartLinks.RuleEngine.Tests.Resolution;

public sealed class UrlResolutionContextFactoryTests
{
    // Добавляет текущее UTC-время через зарегистрированный contributor
    [Fact]
    public void CreateAddsCurrentUtcTimeFeature()
    {
        var utcNow = new DateTimeOffset(2026, 8, 19, 18, 0, 0, TimeSpan.Zero);
        var timeProvider = new StubTimeProvider(utcNow);
        var contributor = new UtcTimeContextContributor(timeProvider);
        var contributors = new IResolutionContextContributor[] { contributor };
        var factory = new UrlResolutionContextFactory(contributors);

        var context = factory.Create(new UrlResolutionRequest());
        var feature = context.GetRequiredFeature<UtcTimeFeature>();

        Assert.Equal(utcNow, feature.UtcNow);
    }

    // Добавляет определённую по IP-адресу страну через зарегистрированный contributor
    [Fact]
    public void CreateAddsCountryFeatureResolvedFromIpAddress()
    {
        var ipAddress = IPAddress.Parse("203.0.113.10");
        const string countryCode = "RU";
        var locationResolver = new StubClientLocationResolver(ipAddress, countryCode);
        var contributor = new CountryContextContributor(locationResolver);
        var contributors = new IResolutionContextContributor[] { contributor };
        var factory = new UrlResolutionContextFactory(contributors);

        var context = factory.Create(new UrlResolutionRequest(ipAddress));
        var feature = context.GetRequiredFeature<CountryFeature>();

        Assert.Equal(countryCode, feature.CountryCode);
    }

    // Создаёт контекст с помощью зарегистрированных contributors
    [Fact]
    public void CreateBuildsContextUsingRegisteredContributors()
    {
        var feature = new StubFeature("value");
        var contributor = new StubResolutionContextContributor(feature);
        var factory = new UrlResolutionContextFactory(new IResolutionContextContributor[] { contributor });

        var context = factory.Create(new UrlResolutionRequest());

        Assert.Same(feature, context.GetRequiredFeature<StubFeature>());
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

    private sealed class StubClientLocationResolver : IClientLocationResolver
    {
        private readonly IPAddress _expectedIpAddress;
        private readonly string _countryCode;

        // Создаёт определитель с ожидаемым IP-адресом и кодом страны
        public StubClientLocationResolver(IPAddress expectedIpAddress, string countryCode)
        {
            _expectedIpAddress = expectedIpAddress;
            _countryCode = countryCode;
        }

        // Возвращает код страны для ожидаемого IP-адреса
        public string? ResolveCountryCode(IPAddress? ipAddress)
        {
            return _expectedIpAddress.Equals(ipAddress) ? _countryCode : null;
        }
    }

    private sealed record StubFeature(string Value) : IResolutionFeature;

    private sealed class StubResolutionContextContributor : IResolutionContextContributor
    {
        private readonly IResolutionFeature _feature;

        // Создаёт contributor с добавляемым признаком
        public StubResolutionContextContributor(IResolutionFeature feature)
        {
            _feature = feature;
        }

        // Добавляет признак в создаваемый контекст
        public void Contribute(UrlResolutionContextBuilder builder, UrlResolutionRequest request)
        {
            builder.Add(_feature);
        }
    }
}