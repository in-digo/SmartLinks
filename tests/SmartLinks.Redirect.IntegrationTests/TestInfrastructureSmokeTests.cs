using System.Reflection;

namespace SmartLinks.Redirect.IntegrationTests;

public sealed class TestInfrastructureSmokeTests
{
    // Проверяет, что интеграционные тесты запускаются и загружают API-сборку Redirect.
    [Fact]
    public void ReferencedAssemblyCanBeLoaded()
    {
        var assembly = Assembly.Load("SmartLinks.Redirect.Api");

        Assert.Equal("SmartLinks.Redirect.Api", assembly.GetName().Name);
    }
}