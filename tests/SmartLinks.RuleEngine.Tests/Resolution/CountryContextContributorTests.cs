using System.Net;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Resolution;

public sealed class CountryContextContributorTests
{
    // Добавляет признак страны, определённой по IP-адресу запроса
    [Fact]
    public void ContributeAddsCountryFeatureResolvedFromIpAddress()
    {
        var ipAddress = IPAddress.Parse("203.0.113.10");
        const string countryCode = "RU";
        var locationResolver = new StubClientLocationResolver(
            ipAddress,
            countryCode);
        var contributor = new CountryContextContributor(locationResolver);
        var builder = new UrlResolutionContextBuilder();

        contributor.Contribute(
            builder,
            new UrlResolutionRequest(ipAddress));

        var context = builder.Build();
        var feature = context.GetRequiredFeature<CountryFeature>();

        Assert.Equal(countryCode, feature.CountryCode);
    }

    private sealed class StubClientLocationResolver : IClientLocationResolver
    {
        private readonly IPAddress _expectedIpAddress;
        private readonly string _countryCode;

        // Создаёт определитель с ожидаемым IP-адресом и кодом страны
        public StubClientLocationResolver(
            IPAddress expectedIpAddress,
            string countryCode)
        {
            _expectedIpAddress = expectedIpAddress;
            _countryCode = countryCode;
        }

        // Возвращает код страны для ожидаемого IP-адреса
        public string? ResolveCountryCode(IPAddress? ipAddress)
        {
            return _expectedIpAddress.Equals(ipAddress)
                ? _countryCode
                : null;
        }
    }
}