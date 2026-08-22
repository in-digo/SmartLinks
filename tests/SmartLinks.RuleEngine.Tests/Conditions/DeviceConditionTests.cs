using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Conditions;

public sealed class DeviceConditionTests
{
    // true, если тип устройства запроса совпадает с типом устройства условия
    [Fact]
    public void IsMatchReturnsTrueWhenDeviceTypeMatches()
    {
        const string deviceType = "mobile";
        var deviceFeature = new DeviceFeature(deviceType);
        var condition = new DeviceCondition(deviceType);
        var context = new UrlResolutionContext(new IResolutionFeature[] { deviceFeature });

        var result = condition.IsMatch(context);

        Assert.True(result);
    }

    // true, если типы устройств отличаются только регистром
    [Fact]
    public void IsMatchReturnsTrueWhenDeviceTypesDifferOnlyByCase()
    {
        var deviceFeature = new DeviceFeature("Mobile");
        var condition = new DeviceCondition("mobile");
        var context = new UrlResolutionContext(new IResolutionFeature[] { deviceFeature });

        var result = condition.IsMatch(context);

        Assert.True(result);
    }
}