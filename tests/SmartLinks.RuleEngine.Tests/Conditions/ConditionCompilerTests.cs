using System.Text.Json;
using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Conditions;

public sealed class ConditionCompilerTests
{
    /// <summary>
    /// Проверяет выбор фабрики, соответствующей типу условия
    /// </summary>
    [Fact]
    public void CompileUsesFactoryMatchingConditionType()
    {
        using var document = JsonDocument.Parse("{}");

        var parameters = document.RootElement.Clone();
        var expectedCondition = new StubCondition(false);
        var unexpectedCondition = new StubCondition(false);
        var countryFactory = new StubConditionFactory("country", unexpectedCondition);
        var timeFactory = new StubConditionFactory("utc-date-time-range", expectedCondition);
        var compiler = new ConditionCompiler([countryFactory, timeFactory]);

        var result = compiler.Compile("utc-date-time-range", parameters);

        Assert.Same(expectedCondition, result);
    }

    /// <summary>
    /// Проверяет ошибку при отсутствии фабрики требуемого типа
    /// </summary>
    [Fact]
    public void CompileThrowsWhenConditionTypeIsNotRegistered()
    {
        using var document = JsonDocument.Parse("{}");

        var parameters = document.RootElement.Clone();
        var compiler = new ConditionCompiler([]);

        var exception = Assert.Throws<InvalidOperationException>(
            () => compiler.Compile("unknown", parameters));

        Assert.Equal("Фабрика условия типа 'unknown' не зарегистрирована", exception.Message);
    }

    private sealed class StubConditionFactory : IConditionFactory
    {
        private readonly ICompiledCondition _condition;

        public string Type { get; }

        /// <summary>
        /// Создаёт фабрику с заданным типом и результатом
        /// </summary>
        public StubConditionFactory(string type, ICompiledCondition condition)
        {
            Type = type;
            _condition = condition;
        }

        /// <summary>
        /// Возвращает заданное скомпилированное условие
        /// </summary>
        public ICompiledCondition Create(JsonElement parameters)
        {
            return _condition;
        }
    }
}