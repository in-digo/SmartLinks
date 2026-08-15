using System.Reflection;

namespace SmartLinks.RuleEngine.Tests;

public sealed class TestInfrastructureSmokeTests
{
    // Проверяет, что тестовый проект запускается и загружает сборку Rule Engine
    [Fact]
    public void ReferencedAssemblyCanBeLoaded()
    {
        var assembly = Assembly.Load("SmartLinks.RuleEngine");

        Assert.Equal("SmartLinks.RuleEngine", assembly.GetName().Name);
    }
}