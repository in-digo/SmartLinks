using System.Reflection;

namespace SmartLinks.Management.IntegrationTests;

public sealed class TestInfrastructureSmokeTests
{
    // Проверяет, что интеграционные тесты запускаются и загружают API-сборку Management
    [Fact]
    public void ReferencedAssemblyCanBeLoaded()
    {
        var assembly = Assembly.Load("SmartLinks.Management.Api");

        Assert.Equal("SmartLinks.Management.Api", assembly.GetName().Name);
    }
}