using System.Reflection;

namespace SmartLinks.Redirect.UnitTests;

public sealed class TestInfrastructureSmokeTests
{
    // Проверяет, что unit-тесты запускаются и загружают application-сборку Redirect.
    [Fact]
    public void ReferencedAssemblyCanBeLoaded()
    {
        var assembly = Assembly.Load("SmartLinks.Redirect.Application");

        Assert.Equal("SmartLinks.Redirect.Application", assembly.GetName().Name);
    }
}