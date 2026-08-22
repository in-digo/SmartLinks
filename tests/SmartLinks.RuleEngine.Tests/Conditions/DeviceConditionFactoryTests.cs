using System.Text.Json;
using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Conditions;

public sealed class DeviceConditionFactoryTests
{
    // Создаёт условие устройства из JSON-параметров
    [Fact]
    public void CreateReturnsConditionUsingConfiguredDeviceType()
    {
        const string parametersJson = """
        {
            "deviceType": "mobile"
        }
        """;

        using var document = JsonDocument.Parse(parametersJson);

        var factory = new DeviceConditionFactory();
        var condition = factory.Create(document.RootElement);
        var deviceFeature = new DeviceFeature("Mobile");
        var context = new UrlResolutionContext(new IResolutionFeature[] { deviceFeature });

        var result = condition.IsMatch(context);

        Assert.True(result);
    }
}