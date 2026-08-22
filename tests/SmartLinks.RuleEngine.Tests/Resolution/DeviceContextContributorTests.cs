using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Resolution;

public sealed class DeviceContextContributorTests
{
    // Добавляет тип устройства, определённый по User-Agent запроса
    [Fact]
    public void ContributeAddsDeviceFeatureResolvedFromUserAgent()
    {
        const string userAgent = "test-user-agent";
        const string deviceType = "mobile";
        var deviceResolver = new StubClientDeviceResolver(userAgent, deviceType);
        var contributor = new DeviceContextContributor(deviceResolver);
        var builder = new UrlResolutionContextBuilder();

        contributor.Contribute(builder, new UrlResolutionRequest(UserAgent: userAgent));

        var context = builder.Build();
        var feature = context.GetRequiredFeature<DeviceFeature>();

        Assert.Equal(deviceType, feature.DeviceType);
    }

    private sealed class StubClientDeviceResolver : IClientDeviceResolver
    {
        private readonly string _expectedUserAgent;
        private readonly string _deviceType;

        // Создаёт определитель с ожидаемым User-Agent и типом устройства
        public StubClientDeviceResolver(string expectedUserAgent, string deviceType)
        {
            _expectedUserAgent = expectedUserAgent;
            _deviceType = deviceType;
        }

        // Возвращает тип устройства для ожидаемого User-Agent
        public string? ResolveDeviceType(string? userAgent)
        {
            return userAgent == _expectedUserAgent ? _deviceType : null;
        }
    }
}