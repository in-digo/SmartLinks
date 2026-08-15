using System.Reflection;

namespace SmartLinks.Management.UnitTests;

public sealed class TestInfrastructureSmokeTests
{
    // Проверяет, что unit-тесты запускаются и загружают application-сборку Management
    [Fact]
    public void ReferencedAssemblyCanBeLoaded()
    {
        var assembly = Assembly.Load("SmartLinks.Management.Application");

        Assert.Equal("SmartLinks.Management.Application", assembly.GetName().Name);
    }
}